using System;
using System.IO;
using WikiTools.Converters;
using Xunit;

namespace WikiTools.Tests;

public class WikidPadToObsidianConverterTests
{
    private readonly string _testDir;
    private readonly string _sourceDir;
    private readonly string _destDir;

    public WikidPadToObsidianConverterTests()
    {
        _testDir = TestUtilities.SetTestFolder();
        _sourceDir = Path.Combine(_testDir, "wp_source");
        _destDir = Path.Combine(_testDir, "obsidian_dest");
    }

    [Fact]
    public void ConvertHeaders_SinglePlus_ToSingleHash()
    {
        // Arrange
        var content = "+ Heading 1";
        var converter = CreateConverter();

        // Act
        var result = converter.ConvertContent(content);

        // Assert
        Assert.Equal("# Heading 1", result);
    }

    [Fact]
    public void ConvertHeaders_DoublePlus_ToDoubleHash()
    {
        // Arrange
        var content = "++ Heading 2\nSome content\n+++ Heading 3";
        var converter = CreateConverter();

        // Act
        var result = converter.ConvertContent(content);

        // Assert
        Assert.Contains("## Heading 2", result);
        Assert.Contains("### Heading 3", result);
    }

    [Fact]
    public void ConvertTags_WikidPadFormat_ToObsidianHash()
    {
        // Arrange
        var content = "Some text [tag:example] more text";
        var converter = CreateConverter();

        // Act
        var result = converter.ConvertContent(content);

        // Assert
        Assert.Contains("#example", result);
        Assert.DoesNotContain("[tag:", result);
    }

    [Fact]
    public void ConvertTags_CategoryFormat_ToObsidianHash()
    {
        // Arrange
        var content = "CategoryTests and more content";
        var converter = CreateConverter();

        // Act
        var result = converter.ConvertContent(content);

        // Assert
        Assert.Contains("#Tests", result);
    }

    [Fact]
    public void ConvertTags_TagWithSpaces_ReplacesWithHyphens()
    {
        // Arrange
        var content = "[tag:my tag name]";
        var converter = CreateConverter();

        // Act
        var result = converter.ConvertContent(content);

        // Assert
        Assert.Equal("#my-tag-name", result);
    }

    [Fact]
    public void ConvertLinks_WikiWord_ToDoubleSquareBrackets()
    {
        // Arrange
        var content = "See [WikiWord] for more info";
        var converter = CreateConverter();

        // Act
        var result = converter.ConvertContent(content);

        // Assert
        Assert.Contains("[[WikiWord]]", result);
    }

    [Fact]
    public void ConvertLinks_AlreadyFormattedLink_RemainsUnchanged()
    {
        // Arrange
        var content = "See [[Link With Spaces]] for more";
        var converter = CreateConverter();

        // Act
        var result = converter.ConvertContent(content);

        // Assert
        Assert.Contains("[[Link With Spaces]]", result);
    }

    [Fact]
    public void ConvertContent_CompleteWikiPage_ConvertsAllElements()
    {
        // Arrange
        var content = @"+ Test Page

This is a test page with [WikiLink] and [[Another Link]].

++ Subsection

Some content here [tag:testing] and CategoryExample.

+++ Details

More details.";

        var converter = CreateConverter();

        // Act
        var result = converter.ConvertContent(content);

        // Assert
        Assert.Contains("# Test Page", result);
        Assert.Contains("## Subsection", result);
        Assert.Contains("### Details", result);
        Assert.Contains("[[WikiLink]]", result);
        Assert.Contains("[[Another Link]]", result);
        Assert.Contains("#testing", result);
        Assert.Contains("#Example", result);
    }

    [Fact]
    public void ConvertAll_CreatesDestinationDirectory()
    {
        // Arrange
        Directory.CreateDirectory(_sourceDir);
        var dataDir = Path.Combine(_sourceDir, "data");
        Directory.CreateDirectory(dataDir);

        File.WriteAllText(Path.Combine(dataDir, "TestPage.wiki"), "+ Test");

        if (Directory.Exists(_destDir))
        {
            Directory.Delete(_destDir, true);
        }

        var converter = new WikidPadToObsidianConverter(_sourceDir, _destDir);

        // Act
        converter.ConvertAll();

        // Assert
        Assert.True(Directory.Exists(_destDir));
    }

    [Fact]
    public void ConvertAll_CreatesMarkdownFiles()
    {
        // Arrange
        Directory.CreateDirectory(_sourceDir);
        var dataDir = Path.Combine(_sourceDir, "data");
        Directory.CreateDirectory(dataDir);

        var testContent = "+ Test Page\n\nContent [tag:test]";
        File.WriteAllText(Path.Combine(dataDir, "TestPage.wiki"), testContent);

        if (Directory.Exists(_destDir))
        {
            Directory.Delete(_destDir, true);
        }

        var converter = new WikidPadToObsidianConverter(_sourceDir, _destDir);

        // Act
        converter.ConvertAll();

        // Assert
        var mdFile = Path.Combine(_destDir, "TestPage.md");
        Assert.True(File.Exists(mdFile));

        var content = File.ReadAllText(mdFile);
        Assert.Contains("# Test Page", content);
        Assert.Contains("#test", content);
    }

    [Fact]
    public void Constructor_ThrowsWhenSourceNotFound()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDir, "does_not_exist");

        // Act & Assert
        Assert.Throws<DirectoryNotFoundException>(() =>
            new WikidPadToObsidianConverter(nonExistentPath, _destDir));
    }

    private WikidPadToObsidianConverter CreateConverter()
    {
        Directory.CreateDirectory(_sourceDir);
        return new WikidPadToObsidianConverter(_sourceDir, _destDir);
    }
}
