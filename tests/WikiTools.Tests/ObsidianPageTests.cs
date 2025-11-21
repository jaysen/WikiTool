using System;
using System.IO;
using WikiTools;
using Xunit;

namespace WikiTools.Tests;

public class ObsidianPageTests
{
    private readonly string _testFolder;

    public ObsidianPageTests()
    {
        _testFolder = TestUtilities.GetTestFolder("obsidian_page_tests");
    }

    [Fact]
    public void Constructor_ThrowsIfFileNotExists()
    {
        // Arrange
        var path = "NoSuchFile.md";

        // Assert
        Assert.Throws<FileNotFoundException>(() => new ObsidianPage(path));
    }

    [Fact]
    public void Constructor_ThrowsIfWrongExtension()
    {
        // Arrange
        var path = Path.Combine(_testFolder, "wrong_extension_test.wiki");
        File.WriteAllText(path, "test");

        // Assert
        Assert.Throws<FormatException>(() => new ObsidianPage(path));
    }

    [Fact]
    public void GetHeaders_ParsesMarkdownHeaders()
    {
        // Arrange
        var path = Path.Combine(_testFolder, "header_test.md");
        var content = @"# Main Header
Some content
## Subheader
### Third Level";
        File.WriteAllText(path, content);

        // Act
        var page = new ObsidianPage(path);
        var headers = page.GetHeaders();

        // Assert
        Assert.Equal(3, headers.Count);
        Assert.Contains("Main Header", headers);
        Assert.Contains("Subheader", headers);
        Assert.Contains("Third Level", headers);
    }

    [Fact]
    public void GetLinks_ParsesWikiLinks()
    {
        // Arrange
        var path = Path.Combine(_testFolder, "links_test.md");
        var content = "See [[Page One]] and [[Page Two|Display Text]] for more.";
        File.WriteAllText(path, content);

        // Act
        var page = new ObsidianPage(path);
        var links = page.GetLinks();

        // Assert
        Assert.Equal(2, links.Count);
        Assert.Contains("Page One", links);
        Assert.Contains("Page Two", links);
    }

    [Fact]
    public void GetTags_ParsesInlineTags()
    {
        // Arrange
        var path = Path.Combine(_testFolder, "inline_tags_test.md");
        var content = "This page has #tag1 and #tag2 tags.";
        File.WriteAllText(path, content);

        // Act
        var page = new ObsidianPage(path);
        var tags = page.GetTags();

        // Assert
        Assert.Equal(2, tags.Count);
        Assert.Contains("tag1", tags);
        Assert.Contains("tag2", tags);
    }

    [Fact]
    public void GetTags_ParsesFrontmatterTags()
    {
        // Arrange
        var path = Path.Combine(_testFolder, "frontmatter_tags_test.md");
        var content = @"---
tags: [example, test]
---

# Content";
        File.WriteAllText(path, content);

        // Act
        var page = new ObsidianPage(path);
        var tags = page.GetTags();

        // Assert
        Assert.Contains("example", tags);
        Assert.Contains("test", tags);
    }

    [Fact]
    public void GetTags_CombinesFrontmatterAndInlineTags()
    {
        // Arrange
        var path = Path.Combine(_testFolder, "combined_tags_test.md");
        var content = @"---
tags: [frontmatter]
---

Content with #inline tag.";
        File.WriteAllText(path, content);

        // Act
        var page = new ObsidianPage(path);
        var tags = page.GetTags();

        // Assert
        Assert.Equal(2, tags.Count);
        Assert.Contains("frontmatter", tags);
        Assert.Contains("inline", tags);
    }

    [Fact]
    public void GetAliases_ParsesFrontmatterAliases()
    {
        // Arrange
        var path = Path.Combine(_testFolder, "aliases_test.md");
        var content = @"---
aliases: [""Alias One"", ""Alias Two""]
---

# Content";
        File.WriteAllText(path, content);

        // Act
        var page = new ObsidianPage(path);
        var aliases = page.GetAliases();

        // Assert
        Assert.Equal(2, aliases.Count);
        Assert.Contains("Alias One", aliases);
        Assert.Contains("Alias Two", aliases);
    }

    [Fact]
    public void GetContent_ReadsFileContent()
    {
        // Arrange
        var path = Path.Combine(_testFolder, "content_test.md");
        var expected = "# Test\n\nSome content here.";
        File.WriteAllText(path, expected);

        // Act
        var page = new ObsidianPage(path);
        var actual = page.GetContent();

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GetAttributes_ParsesInlineAttributes()
    {
        // Arrange
        var path = Path.Combine(_testFolder, "attributes_inline_test.md");
        var content = "Some text [author:: John Doe] more text [status:: draft]";
        File.WriteAllText(path, content);

        // Act
        var wiki = new ObsidianWiki(_testFolder);
        var sut = new ObsidianPage(path, wiki);
        var attributes = sut.GetAttributes();

        // Assert
        Assert.Equal(2, attributes.Count);
        Assert.Equal("John Doe", attributes["author"]);
        Assert.Equal("draft", attributes["status"]);
    }

    [Fact]
    public void GetAttributes_ParsesYamlFrontmatterAttributes()
    {
        // Arrange
        var path = Path.Combine(_testFolder, "attributes_yaml_test.md");
        var content = @"---
author: John Doe
status: draft
date: 2024-01-15
---
Content here";
        File.WriteAllText(path, content);

        // Act
        var wiki = new ObsidianWiki(_testFolder);
        var sut = new ObsidianPage(path, wiki);
        var attributes = sut.GetAttributes();

        // Assert
        Assert.Equal(3, attributes.Count);
        Assert.Equal("John Doe", attributes["author"]);
        Assert.Equal("draft", attributes["status"]);
        Assert.Equal("2024-01-15", attributes["date"]);
    }

    [Fact]
    public void GetAttributes_ParsesBothInlineAndYaml()
    {
        // Arrange
        var path = Path.Combine(_testFolder, "attributes_combined_test.md");
        var content = @"---
yaml-attr: from yaml
---
Content [inline-attr:: from inline]";
        File.WriteAllText(path, content);

        // Act
        var wiki = new ObsidianWiki(_testFolder);
        var sut = new ObsidianPage(path, wiki);
        var attributes = sut.GetAttributes();

        // Assert
        Assert.Equal(2, attributes.Count);
        Assert.Equal("from yaml", attributes["yaml-attr"]);
        Assert.Equal("from inline", attributes["inline-attr"]);
    }

    [Fact]
    public void GetAttributes_SkipsTagsAndAliasesInYaml()
    {
        // Arrange
        var path = Path.Combine(_testFolder, "attributes_yaml_skip_test.md");
        var content = @"---
tags: [tag1, tag2]
aliases: [alias1]
author: John
---
Content";
        File.WriteAllText(path, content);

        // Act
        var wiki = new ObsidianWiki(_testFolder);
        var sut = new ObsidianPage(path, wiki);
        var attributes = sut.GetAttributes();

        // Assert
        Assert.Equal(1, attributes.Count);
        Assert.Equal("John", attributes["author"]);
        Assert.False(attributes.ContainsKey("tags"));
        Assert.False(attributes.ContainsKey("aliases"));
    }

    [Fact]
    public void GetAttributes_ReturnsEmptyWhenNoAttributes()
    {
        // Arrange
        var path = Path.Combine(_testFolder, "attributes_empty_test.md");
        var content = "Just some plain text";
        File.WriteAllText(path, content);

        // Act
        var wiki = new ObsidianWiki(_testFolder);
        var sut = new ObsidianPage(path, wiki);
        var attributes = sut.GetAttributes();

        // Assert
        Assert.Equal(0, attributes.Count);
    }

    [Fact]
    public void GetAttributes_CleansUpArrayValues()
    {
        // Arrange
        var path = Path.Combine(_testFolder, "attributes_array_test.md");
        var content = @"---
categories: [cat1, cat2]
---
Content";
        File.WriteAllText(path, content);

        // Act
        var wiki = new ObsidianWiki(_testFolder);
        var sut = new ObsidianPage(path, wiki);
        var attributes = sut.GetAttributes();

        // Assert
        Assert.Equal(1, attributes.Count);
        Assert.Equal("cat1, cat2", attributes["categories"]);
    }

    [Fact]
    public void GetAttributes_YamlOverridesInlineForSameKey()
    {
        // Arrange
        var path = Path.Combine(_testFolder, "attributes_override_test.md");
        var content = @"---
status: yaml
---
Content [status:: inline]";
        File.WriteAllText(path, content);

        // Act
        var wiki = new ObsidianWiki(_testFolder);
        var sut = new ObsidianPage(path, wiki);
        var attributes = sut.GetAttributes();

        // Assert
        Assert.Equal(1, attributes.Count);
        // Inline is processed first, then YAML overwrites it
        Assert.Equal("yaml", attributes["status"]);
    }
}
