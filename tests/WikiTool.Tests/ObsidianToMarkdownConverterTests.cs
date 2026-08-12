using System;
using System.IO;
using System.Linq;
using WikiTool.Converters;
using Xunit;

namespace WikiTool.Tests;

/// <summary>
/// Covers the Obsidian -> Markdown wiki conversion. Each test builds a throwaway vault so
/// the two-pass link resolution has a real multi-page vault to resolve against.
/// </summary>
public class ObsidianToMarkdownConverterTests : IDisposable
{
    private readonly string _root;
    private readonly string _source;
    private readonly string _dest;

    public ObsidianToMarkdownConverterTests()
    {
        _root = Path.Combine(
            TestUtilities.GetTestFolder("obsidian_markdown_tests"),
            Guid.NewGuid().ToString("N"));

        _source = Path.Combine(_root, "source");
        _dest = Path.Combine(_root, "dest");

        Directory.CreateDirectory(_source);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
    }

    private void WritePage(string relativePath, string content)
    {
        var path = Path.Combine(_source, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, content);
    }

    private string ReadOutput(string relativePath)
    {
        return File.ReadAllText(Path.Combine(_dest, relativePath.Replace('/', Path.DirectorySeparatorChar)));
    }

    private ObsidianToMarkdownConverter Convert(bool scaffolding = false)
    {
        var converter = new ObsidianToMarkdownConverter(_source, _dest)
        {
            GenerateSiteFiles = scaffolding
        };
        converter.ConvertAll();
        return converter;
    }

    #region Link conversion

    [Fact]
    public void SimpleWikiLink_BecomesRelativeMarkdownLink()
    {
        WritePage("Alpha.md", "See [[Beta]].");
        WritePage("Beta.md", "Beta page.");

        Convert();

        Assert.Contains("[Beta](beta.md)", ReadOutput("alpha.md"));
    }

    [Fact]
    public void WikiLinkWithDisplayText_UsesDisplayText()
    {
        WritePage("Alpha.md", "See [[Beta|the other page]].");
        WritePage("Beta.md", "Beta page.");

        Convert();

        Assert.Contains("[the other page](beta.md)", ReadOutput("alpha.md"));
    }

    [Fact]
    public void WikiLinkAcrossFolders_UsesRelativePath()
    {
        WritePage("Projects/Alpha.md", "See [[C# Notes]].");
        WritePage("Notes/C# Notes.md", "Notes page.");

        Convert();

        Assert.Contains("(../notes/c-sharp-notes.md)", ReadOutput("projects/alpha.md"));
    }

    [Fact]
    public void WikiLinkWithHeading_LinksToAnchorAndShowsBreadcrumb()
    {
        WritePage("Alpha.md", "See [[Beta#Getting Started]].");
        WritePage("Beta.md", "## Getting Started");

        Convert();

        Assert.Contains("[Beta > Getting Started](beta.md#getting-started)", ReadOutput("alpha.md"));
    }

    [Fact]
    public void SamePageHeadingLink_BecomesBareAnchor()
    {
        WritePage("Alpha.md", "Jump to [[#Overview]].\n\n## Overview");

        Convert();

        Assert.Contains("[Overview](#overview)", ReadOutput("alpha.md"));
    }

    [Fact]
    public void BlockReference_LinksToThePageWithoutAnAnchor()
    {
        WritePage("Alpha.md", "See [[Beta#^abc123]].");
        WritePage("Beta.md", "Beta page.");

        Convert();

        var output = ReadOutput("alpha.md");
        Assert.Contains("[Beta](beta.md)", output);
        Assert.DoesNotContain("abc123", output);
    }

    [Fact]
    public void LinkByPath_ResolvesToTheRightPage()
    {
        WritePage("Projects/Alpha.md", "See [[Notes/Beta]].");
        WritePage("Notes/Beta.md", "Beta page.");

        Convert();

        Assert.Contains("(../notes/beta.md)", ReadOutput("projects/alpha.md"));
    }

    /// <summary>
    /// Obsidian resolves link targets case-insensitively.
    /// </summary>
    [Fact]
    public void LinkWithDifferentCasing_StillResolves()
    {
        WritePage("Alpha.md", "See [[bETa]].");
        WritePage("Beta.md", "Beta page.");

        Convert();

        Assert.Contains("(beta.md)", ReadOutput("alpha.md"));
    }

    [Fact]
    public void LinkViaAlias_ResolvesToTheAliasedPage()
    {
        WritePage("Alpha.md", "See [[The Second Page]].");
        WritePage("Beta.md", "---\naliases: [The Second Page]\n---\nBeta page.");

        Convert();

        Assert.Contains("(beta.md)", ReadOutput("alpha.md"));
    }

    /// <summary>
    /// A dead link must not become an href that 404s on the published site.
    /// </summary>
    [Fact]
    public void UnresolvedLink_BecomesBoldTextAndIsReported()
    {
        WritePage("Alpha.md", "See [[Nowhere]].");

        var converter = Convert();

        var output = ReadOutput("alpha.md");
        Assert.Contains("**Nowhere**", output);
        Assert.DoesNotContain("[[", output);
        Assert.Contains(converter.Warnings, w => w.Contains("Nowhere"));
    }

    /// <summary>
    /// Embeds are explicitly out of scope, so they must pass through whole rather than
    /// being half-converted into a broken image link.
    /// </summary>
    [Fact]
    public void Embed_IsLeftUntouched()
    {
        WritePage("Alpha.md", "![[diagram.png]]");

        Convert();

        Assert.Contains("![[diagram.png]]", ReadOutput("alpha.md"));
    }

    #endregion

    #region Code protection

    [Fact]
    public void WikiLinksInsideFencedCode_AreNotConverted()
    {
        WritePage("Alpha.md", "Real: [[Beta]]\n\n```\nExample: [[Beta]] and #nottag\n```\n");
        WritePage("Beta.md", "Beta page.");

        Convert();

        var output = ReadOutput("alpha.md");
        Assert.Contains("Example: [[Beta]] and #nottag", output);
        Assert.Contains("Real: [Beta](beta.md)", output);
    }

    /// <summary>
    /// Whitespace tidying must run before the stashed code is put back, or it would
    /// reformat the inside of code blocks.
    /// </summary>
    [Fact]
    public void IndentationInsideFencedCode_IsPreserved()
    {
        WritePage("Alpha.md", "Text #tag\n\n```python\ndef f():\n    if x:\n        return  1\n```\n");

        Convert();

        var output = ReadOutput("alpha.md");
        Assert.Contains("    if x:", output);
        Assert.Contains("        return  1", output);
    }

    [Fact]
    public void WikiLinksInsideInlineCode_AreNotConverted()
    {
        WritePage("Alpha.md", "Write `[[Beta]]` to link. Real: [[Beta]]");
        WritePage("Beta.md", "Beta page.");

        Convert();

        var output = ReadOutput("alpha.md");
        Assert.Contains("`[[Beta]]`", output);
        Assert.Contains("Real: [Beta](beta.md)", output);
    }

    #endregion

    #region Tags

    [Fact]
    public void InlineTags_AreHoistedToFrontmatterAndRemovedFromBody()
    {
        WritePage("Alpha.md", "Body text #project #status/wip");

        Convert();

        var output = ReadOutput("alpha.md");
        Assert.Contains("tags:\n  - project\n  - status/wip", output.Replace("\r\n", "\n"));
        Assert.DoesNotContain("#project", output);
    }

    /// <summary>
    /// Removing a tag from between two words must not leave a doubled space behind.
    /// </summary>
    [Fact]
    public void StrippingAnInlineTag_DoesNotLeaveDoubledSpaces()
    {
        WritePage("Alpha.md", "Some #project text here.");

        Convert();

        Assert.Contains("Some text here.", ReadOutput("alpha.md"));
    }

    [Fact]
    public void FrontmatterAndInlineTags_AreMerged()
    {
        WritePage("Alpha.md", "---\ntags: [fromfront]\n---\nBody #frombody");

        Convert();

        var output = ReadOutput("alpha.md");
        Assert.Contains("- fromfront", output);
        Assert.Contains("- frombody", output);
    }

    [Fact]
    public void BlockListFrontmatterTags_AreRead()
    {
        WritePage("Alpha.md", "---\ntags:\n  - one\n  - two\n---\nBody");

        Convert();

        var output = ReadOutput("alpha.md");
        Assert.Contains("- one", output);
        Assert.Contains("- two", output);
    }

    [Fact]
    public void KeepInlineTags_LeavesTheBodyAlone()
    {
        WritePage("Alpha.md", "Body text #project");

        var converter = new ObsidianToMarkdownConverter(_source, _dest)
        {
            GenerateSiteFiles = false,
            StripInlineTags = false
        };
        converter.ConvertAll();

        Assert.Contains("Body text #project", ReadOutput("alpha.md"));
    }

    /// <summary>
    /// An ATX heading is not a tag.
    /// </summary>
    [Fact]
    public void Headings_AreNotTreatedAsTags()
    {
        WritePage("Alpha.md", "# Heading One\n\n## Heading Two\n");

        Convert();

        var output = ReadOutput("alpha.md");
        Assert.Contains("# Heading One", output);
        Assert.Contains("## Heading Two", output);
    }

    #endregion

    #region Frontmatter

    [Fact]
    public void Title_PreservesTheOriginalUnSluggedName()
    {
        WritePage("C# Notes.md", "Body");

        Convert();

        Assert.Contains("title: \"C# Notes\"", ReadOutput("c-sharp-notes.md"));
    }

    /// <summary>
    /// A title the author set explicitly must survive, rather than being replaced by
    /// whatever filename the note happened to be stored under.
    /// </summary>
    [Fact]
    public void ExplicitFrontmatterTitle_OutranksTheFilename()
    {
        WritePage("Alpha.md", "---\ntitle: My Real Title\n---\nBody");

        Convert();

        var output = ReadOutput("alpha.md");
        Assert.Contains("title: \"My Real Title\"", output);
        Assert.DoesNotContain("\"Alpha\"", output);
    }

    [Fact]
    public void UnmanagedFrontmatterKeys_ArePassedThrough()
    {
        WritePage("Alpha.md", "---\nauthor: Jaysen\nstatus: draft\n---\nBody");

        Convert();

        var output = ReadOutput("alpha.md");
        Assert.Contains("author: Jaysen", output);
        Assert.Contains("status: draft", output);
    }

    [Fact]
    public void Aliases_BecomeJekyllRedirects()
    {
        WritePage("Alpha.md", "---\naliases:\n  - Old Name\n---\nBody");

        Convert();

        Assert.Contains("redirect_from:\n  - /old-name/", ReadOutput("alpha.md").Replace("\r\n", "\n"));
    }

    /// <summary>
    /// A redirect that would shadow a real page must be skipped rather than break its URL.
    /// </summary>
    [Fact]
    public void AliasCollidingWithAnotherPage_DoesNotGenerateARedirect()
    {
        WritePage("Alpha.md", "---\naliases: [Beta]\n---\nBody");
        WritePage("Beta.md", "Beta page.");

        var converter = Convert();

        Assert.DoesNotContain("redirect_from", ReadOutput("alpha.md"));
        Assert.Contains(converter.Warnings, w => w.Contains("Beta"));
    }

    #endregion

    #region Vault handling

    [Fact]
    public void ObsidianConfigFolder_IsExcluded()
    {
        WritePage("Alpha.md", "Body");
        WritePage(".obsidian/plugins/notes.md", "Should not be converted");

        Convert();

        Assert.True(File.Exists(Path.Combine(_dest, "alpha.md")));
        Assert.False(Directory.Exists(Path.Combine(_dest, "obsidian")));
    }

    [Fact]
    public void DuplicatePageNames_AreReportedAndBothWritten()
    {
        WritePage("One/Shared.md", "First");
        WritePage("Two/Shared.md", "Second");

        var converter = Convert();

        Assert.True(File.Exists(Path.Combine(_dest, "one", "shared.md")));
        Assert.True(File.Exists(Path.Combine(_dest, "two", "shared.md")));
        Assert.Contains(converter.Warnings, w => w.Contains("Duplicate page name"));
    }

    #endregion

    #region Site scaffolding

    [Fact]
    public void Scaffolding_WritesConfigAndBothIndexes()
    {
        WritePage("Alpha.md", "Body #project");

        Convert(scaffolding: true);

        var config = ReadOutput("_config.yml");
        Assert.Contains("jekyll-relative-links", config);
        Assert.Contains("relative_links:", config);

        Assert.Contains("[Alpha](../alpha.md)", ReadOutput("indexes/page-index.md"));

        var tags = ReadOutput("indexes/tag-index.md");
        Assert.Contains("## project", tags);
        Assert.Contains("[Alpha](../alpha.md)", tags);
    }

    /// <summary>
    /// index.md becomes index.html and belongs to the wiki's own home page, so the
    /// generated listings must never claim it.
    /// </summary>
    [Fact]
    public void Scaffolding_NeverWritesAnIndexFileAtTheSiteRoot()
    {
        WritePage("Alpha.md", "Body");

        Convert(scaffolding: true);

        Assert.False(File.Exists(Path.Combine(_dest, "index.md")));
        Assert.False(File.Exists(Path.Combine(_dest, "tags.md")));
    }

    [Fact]
    public void Scaffolding_LinksBetweenTheTwoIndexes()
    {
        WritePage("Alpha.md", "Body #project");

        Convert(scaffolding: true);

        Assert.Contains("[tag index](tag-index.md)", ReadOutput("indexes/page-index.md"));
    }

    /// <summary>
    /// Index links are relative to the index folder, not the site root.
    /// </summary>
    [Fact]
    public void IndexLinks_ResolveFromInsideTheIndexFolder()
    {
        WritePage("Projects/Alpha.md", "Body #project");

        Convert(scaffolding: true);

        Assert.Contains("(../projects/alpha.md)", ReadOutput("indexes/page-index.md"));
        Assert.Contains("(../projects/alpha.md)", ReadOutput("indexes/tag-index.md"));
    }

    [Fact]
    public void IndexFolder_CanBeChanged()
    {
        WritePage("Alpha.md", "Body");

        var converter = new ObsidianToMarkdownConverter(_source, _dest)
        {
            IndexFolder = "Site Listings"
        };
        converter.ConvertAll();

        Assert.True(File.Exists(Path.Combine(_dest, "site-listings", "page-index.md")));
    }

    [Fact]
    public void EmptyIndexFolder_WritesIndexesAtTheRoot()
    {
        WritePage("Alpha.md", "Body");

        var converter = new ObsidianToMarkdownConverter(_source, _dest)
        {
            IndexFolder = ""
        };
        converter.ConvertAll();

        Assert.Contains("[Alpha](alpha.md)", ReadOutput("page-index.md"));
    }

    /// <summary>
    /// A page the vault itself produced at index.md is the site home page and must survive.
    /// </summary>
    [Fact]
    public void ConvertedHomePage_IsLeftAlone()
    {
        WritePage("index.md", "My own home page");

        Convert(scaffolding: true);

        Assert.Contains("My own home page", ReadOutput("index.md"));
    }

    [Fact]
    public void Scaffolding_DoesNotOverwriteAnExistingConfigWithoutForce()
    {
        WritePage("Alpha.md", "Body");
        Directory.CreateDirectory(_dest);
        File.WriteAllText(Path.Combine(_dest, "_config.yml"), "title: Hand written");

        var converter = Convert(scaffolding: true);

        Assert.Equal("title: Hand written", ReadOutput("_config.yml"));
        Assert.Contains(converter.Warnings, w => w.Contains("_config.yml"));
    }

    #endregion

    /// <summary>
    /// The end state that matters: no Obsidian link syntax survives outside embeds and code.
    /// </summary>
    [Fact]
    public void ConvertedVault_ContainsNoStrayWikiLinkSyntax()
    {
        WritePage("Home.md", "[[Projects/Alpha]] and [[Beta|beta]] and [[Nowhere]]");
        WritePage("Projects/Alpha.md", "[[Home]] #project");
        WritePage("Beta.md", "[[Home#Top]]");

        Convert(scaffolding: true);

        var stray = Directory
            .EnumerateFiles(_dest, "*.md", SearchOption.AllDirectories)
            .Where(f => File.ReadAllText(f).Contains("[["))
            .ToList();

        Assert.Empty(stray);
    }
}
