using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using WikiTool.Wikis;

namespace WikiTool.Converters;

/// <summary>
/// Converts an Obsidian vault into a plain Markdown wiki that publishes to GitHub Pages.
///
/// Unlike <see cref="WikidPadToObsidianConverter"/>, this converter is two-pass: Obsidian
/// resolves [[links]] by page name against the whole vault, so <see cref="VaultIndex"/> must
/// see every page before any page can be rewritten.
///
/// Output uses relative "page.md" links. GitHub Pages enables the jekyll-relative-links
/// plugin by default, which rewrites those to .html at build time - so the same files read
/// correctly both when browsing the repo and when served from Pages.
/// </summary>
public partial class ObsidianToMarkdownConverter
{
    public string SourcePath { get; }
    public string DestinationPath { get; }

    /// <summary>
    /// When true, inline #tags are removed from the body and hoisted into YAML frontmatter,
    /// where Jekyll can use them. Default is true - a literal "#tag" renders as noise on a
    /// published site.
    /// </summary>
    public bool StripInlineTags { get; set; } = true;

    /// <summary>
    /// When true, writes _config.yml, index.md and tags.md alongside the converted pages.
    /// Default is true.
    /// </summary>
    public bool GenerateSiteFiles { get; set; } = true;

    /// <summary>
    /// Site title written into _config.yml.
    /// </summary>
    public string SiteTitle { get; set; }

    /// <summary>
    /// Allows overwriting an existing _config.yml in the destination. Default is false.
    /// </summary>
    public bool Force { get; set; } = false;

    /// <summary>
    /// Warnings collected during the conversion: duplicate page names, unresolved links, and
    /// filename collisions. Populated by <see cref="ConvertAll"/>.
    /// </summary>
    public List<string> Warnings { get; } = new List<string>();

    private VaultIndex _index;

    /// <summary>
    /// Matches an Obsidian wikilink.
    /// The (?&lt;!!) lookbehind is load-bearing: it leaves embeds (![[image.png]]) untouched
    /// rather than half-converting them, since embeds are out of scope.
    /// Group 1 target, group 2 heading or ^blockref, group 3 display text.
    /// </summary>
    [GeneratedRegex(@"(?<!!)\[\[([^\[\]|#\r\n]*)(?:#([^\[\]|\r\n]*))?(?:\|([^\[\]\r\n]*))?\]\]")]
    private static partial Regex WikiLinkRegex();

    /// <summary>
    /// Matches three or more consecutive newlines, left behind after stripping inline tags.
    /// </summary>
    [GeneratedRegex(@"(\r?\n){3,}")]
    private static partial Regex ExcessBlankLinesRegex();

    /// <summary>
    /// Matches trailing spaces at the end of a line.
    /// </summary>
    [GeneratedRegex(@"[ \t]+(\r?\n|$)")]
    private static partial Regex TrailingSpaceRegex();

    /// <summary>
    /// Matches a run of spaces in the middle of a line, as left behind when an inline tag
    /// is removed from between two words. The lookbehind protects leading indentation,
    /// which is meaningful for nested lists.
    /// </summary>
    [GeneratedRegex(@"(?<=\S)[ ]{2,}")]
    private static partial Regex InteriorSpaceRunRegex();

    public ObsidianToMarkdownConverter(string sourcePath, string destinationPath)
    {
        if (!Directory.Exists(sourcePath))
        {
            throw new DirectoryNotFoundException($"Source directory not found: {sourcePath}");
        }

        SourcePath = sourcePath;
        DestinationPath = destinationPath;
    }

    /// <summary>
    /// Convert the whole vault: index it, rewrite every page, then write the site scaffolding.
    /// </summary>
    public void ConvertAll()
    {
        _index = VaultIndex.Build(SourcePath);
        Warnings.AddRange(_index.Warnings);

        Directory.CreateDirectory(DestinationPath);

        foreach (var page in _index.Pages)
        {
            var content = File.ReadAllText(page.SourcePath);
            var converted = ConvertPage(content, page);

            var outputPath = Path.Combine(DestinationPath, page.OutputRelPath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            File.WriteAllText(outputPath, converted);
        }

        if (GenerateSiteFiles)
        {
            var writer = new JekyllSiteWriter(DestinationPath, _index)
            {
                SiteTitle = SiteTitle ?? new DirectoryInfo(SourcePath).Name,
                Force = Force
            };
            writer.Write();
            Warnings.AddRange(writer.Warnings);
        }
    }

    /// <summary>
    /// Convert a single page's content. Requires the vault to have been indexed, which
    /// <see cref="ConvertAll"/> does; <see cref="ConvertContent"/> is the entry point for
    /// converting a standalone string against an index built separately.
    /// </summary>
    public string ConvertContent(string content, VaultIndex index, VaultPage page)
    {
        _index = index;
        return ConvertPage(content, page);
    }

    private string ConvertPage(string content, VaultPage page)
    {
        if (string.IsNullOrEmpty(content))
        {
            return content;
        }

        var frontmatter = YamlFrontmatter.Parse(content);
        var body = frontmatter.Body;

        // Hold code back before any rewriting, so [[links]] and #tags shown as examples survive
        var stash = new CodeStash();
        body = stash.Hide(body);

        body = ConvertLinks(body, page);

        var tags = new List<string>(frontmatter.GetList("tags"));
        body = CollectAndStripTags(body, tags);

        // Record what this page actually ended up tagged with, so the generated tag index
        // matches the pages rather than a separate scan that also sees code samples
        page.Tags = tags;

        // Tidy before restoring, so whitespace inside code blocks is never touched
        body = Tidy(body);
        body = stash.Restore(body);

        return BuildFrontmatter(frontmatter, page, tags) + body;
    }

    /// <summary>
    /// Rewrite [[wikilinks]] into standard Markdown links relative to this page.
    /// </summary>
    private string ConvertLinks(string body, VaultPage page)
    {
        return WikiLinkRegex().Replace(body, match =>
        {
            var target = match.Groups[1].Value.Trim();
            var heading = match.Groups[2].Success ? match.Groups[2].Value.Trim() : null;
            var display = match.Groups[3].Success ? match.Groups[3].Value.Trim() : null;

            // Block references ([[Page#^blockid]]) have no Markdown equivalent; link to the page
            var isBlockRef = heading != null && heading.StartsWith("^");
            if (isBlockRef)
            {
                heading = null;
            }

            // '#' is both the heading separator and a legal filename character, so a link like
            // [[C# Notes]] splits wrongly. If the whole literal names a page, that wins.
            if (heading != null && target.Length > 0)
            {
                // Use the raw heading group: trimming would turn "C# Notes" into "C#Notes"
                var literal = $"{match.Groups[1].Value}#{match.Groups[2].Value}".Trim();
                if (_index.TryResolve(literal, out _))
                {
                    target = literal;
                    heading = null;
                }
            }

            // Same-page heading link: [[#Setup]]
            if (target.Length == 0)
            {
                if (heading == null)
                {
                    return match.Value;
                }

                return $"[{display ?? heading}](#{Slug.ForAnchor(heading)})";
            }

            if (!_index.TryResolve(target, out var targetPage))
            {
                Warnings.Add($"{page.SourceRelPath}: unresolved link [[{target}]]");

                // Emit plain bold text rather than a link that would 404 on the published site
                return $"**{display ?? target}**";
            }

            var href = MakeRelative(page.OutputRelPath, targetPage.OutputRelPath);
            if (heading != null && heading.Length > 0)
            {
                href += "#" + Slug.ForAnchor(heading);
            }

            var text = display ?? (heading != null && heading.Length > 0
                ? $"{target} > {heading}"
                : target);

            return $"[{text}]({href})";
        });
    }

    /// <summary>
    /// Gather inline #tags into the supplied list, removing them from the body when
    /// <see cref="StripInlineTags"/> is set.
    /// </summary>
    private string CollectAndStripTags(string body, List<string> tags)
    {
        var pattern = ObsidianSyntax.Default.TagPattern;

        return pattern.Replace(body, match =>
        {
            var tag = match.Groups[1].Value;

            if (!tags.Contains(tag))
            {
                tags.Add(tag);
            }

            if (!StripInlineTags)
            {
                return match.Value;
            }

            // The match consumes the whitespace before the hash; keep it so words stay separated
            return match.Value.Substring(0, match.Value.Length - tag.Length - 1);
        });
    }

    /// <summary>
    /// Compose the output frontmatter: generated keys first, then everything the source
    /// page carried that this converter does not manage, verbatim.
    /// </summary>
    private string BuildFrontmatter(YamlFrontmatter source, VaultPage page, List<string> tags)
    {
        var sb = new StringBuilder();
        sb.AppendLine("---");

        // A title the author set explicitly outranks the filename it was stored under
        var title = source.GetScalar("title");
        sb.AppendLine($"title: {QuoteScalar(string.IsNullOrEmpty(title) ? page.Title : title)}");

        if (tags.Count > 0)
        {
            sb.AppendLine("tags:");
            foreach (var tag in tags)
            {
                sb.AppendLine($"  - {tag}");
            }
        }

        var redirects = BuildRedirects(page);
        if (redirects.Count > 0)
        {
            sb.AppendLine("redirect_from:");
            foreach (var redirect in redirects)
            {
                sb.AppendLine($"  - {redirect}");
            }
        }

        foreach (var entry in source.Entries)
        {
            if (IsManagedKey(entry.Key))
            {
                continue;
            }

            foreach (var line in entry.RawLines)
            {
                sb.AppendLine(line);
            }
        }

        sb.AppendLine("---");
        return sb.ToString();
    }

    private static bool IsManagedKey(string key)
    {
        return string.Equals(key, "title", StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, "tags", StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, "aliases", StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, "redirect_from", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Turn Obsidian aliases into jekyll-redirect-from entries, so links to a page's old
    /// names keep working. An alias whose URL would collide with a real page is skipped
    /// rather than shadowing that page.
    /// </summary>
    private List<string> BuildRedirects(VaultPage page)
    {
        var redirects = new List<string>();

        foreach (var alias in page.Aliases)
        {
            var slug = Slug.ForFile(alias);

            if (_index.TryResolve(alias, out var owner) && owner != page)
            {
                Warnings.Add($"{page.SourceRelPath}: alias '{alias}' also names another page; redirect skipped.");
                continue;
            }

            var url = "/" + slug + "/";
            if (!redirects.Contains(url))
            {
                redirects.Add(url);
            }
        }

        return redirects;
    }

    /// <summary>
    /// Quote a scalar so a title containing ':' or '#' cannot break the YAML.
    /// </summary>
    private static string QuoteScalar(string value)
    {
        return "\"" + (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }

    /// <summary>
    /// Build a link from one output page to another, as a relative path with '/' separators.
    /// Slugs contain no characters needing URL escaping.
    /// </summary>
    internal static string MakeRelative(string fromPage, string toPage)
    {
        var fromDir = SplitDirectory(fromPage);
        var toParts = toPage.Split('/');

        var common = 0;
        while (common < fromDir.Length
               && common < toParts.Length - 1
               && string.Equals(fromDir[common], toParts[common], StringComparison.OrdinalIgnoreCase))
        {
            common++;
        }

        var segments = new List<string>();
        for (var i = common; i < fromDir.Length; i++)
        {
            segments.Add("..");
        }

        for (var i = common; i < toParts.Length; i++)
        {
            segments.Add(toParts[i]);
        }

        return string.Join("/", segments);
    }

    private static string[] SplitDirectory(string path)
    {
        var index = path.LastIndexOf('/');
        return index < 0
            ? Array.Empty<string>()
            : path.Substring(0, index).Split('/');
    }

    /// <summary>
    /// Clean up whitespace left behind by removing inline tags.
    /// </summary>
    private static string Tidy(string body)
    {
        body = InteriorSpaceRunRegex().Replace(body, " ");
        body = TrailingSpaceRegex().Replace(body, "$1");
        body = ExcessBlankLinesRegex().Replace(body, "$1$1");
        return body.TrimStart('\r', '\n');
    }
}
