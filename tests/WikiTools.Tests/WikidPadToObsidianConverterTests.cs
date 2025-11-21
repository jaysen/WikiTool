using System;
using System.IO;
using WikiTools.Converters;
using Xunit;

namespace WikiTools.Tests;

public class WikidPadToObsidianConverterTests
{
    private readonly string _testFolder;
    private readonly string _sourceDir;
    private readonly string _destDir;

    public WikidPadToObsidianConverterTests()
    {
        _testFolder = TestUtilities.GetTestFolder("wikidpad_converter_tests");
        _sourceDir = Path.Combine(_testFolder, "source");
        _destDir = Path.Combine(_testFolder, "dest");
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
    public void ConvertTags_MultipleAdjacentTags_HasSpacesBetweenHashtags()
    {
        // Arrange
        var content = "start [tag:one][tag:two]CategoryThree [tag:four] and continue";
        var converter = CreateConverter();

        // Act
        var result = converter.ConvertContent(content);

        // Assert
        Assert.Equal("start #one #two #Three #four and continue", result);
        Assert.DoesNotContain("#one#two", result);
        Assert.DoesNotContain("#two#Three", result);
        Assert.DoesNotContain("#Three#four", result);
    }

    [Fact]
    public void ConvertLinks_BareCamelCase_ToDoubleSquareBrackets()
    {
        // Arrange
        var content = "See WikiWord for more info";
        var converter = CreateConverter();

        // Act
        var result = converter.ConvertContent(content);

        // Assert
        Assert.Contains("[[WikiWord]]", result);
    }

    [Fact]
    public void ConvertLinks_VariousCamelCasePatterns_ToDoubleSquareBrackets()
    {
        // Arrange - Test all the WikidPad CamelCase patterns
        var content = "Test AbC and AbcD and ABcd and ABcD and AbCDe here";
        var converter = CreateConverter();

        // Act
        var result = converter.ConvertContent(content);

        // Assert
        Assert.Contains("[[AbC]]", result);
        Assert.Contains("[[AbcD]]", result);
        Assert.Contains("[[ABcd]]", result);
        Assert.Contains("[[ABcD]]", result);
        Assert.Contains("[[AbCDe]]", result);
    }

    [Fact]
    public void ConvertLinks_CamelCaseWithNumbers_ToDoubleSquareBrackets()
    {
        // Arrange
        var content = "Test AbC123 and AbcD45 and ABcd6 and ABcD789 and AbCDe0 here";
        var converter = CreateConverter();

        // Act
        var result = converter.ConvertContent(content);

        // Assert
        Assert.Contains("[[AbC123]]", result);
        Assert.Contains("[[AbcD45]]", result);
        Assert.Contains("[[ABcd6]]", result);
        Assert.Contains("[[ABcD789]]", result);
        Assert.Contains("[[AbCDe0]]", result);
    }

    [Fact]
    public void ConvertLinks_SingleBracketLink_ToDoubleSquareBrackets()
    {
        // Arrange
        var content = "See [Link with Spaces] for more info";
        var converter = CreateConverter();

        // Act
        var result = converter.ConvertContent(content);

        // Assert
        Assert.Equal("See [[Link with Spaces]] for more info", result);
    }

    [Fact]
    public void ConvertLinks_SingleBracketCamelCase_ToDoubleSquareBrackets()
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
        Assert.Contains("See [[Link With Spaces]] for more", result);
    }

    [Fact]
    public void ConvertContent_CompleteWikiPage_ConvertsAllElements()
    {
        // Arrange
        var content = @"+ Test Page

This is a test page with WikiLink, [Another Link], and [[Already Done]].

++ Subsection

Some content here [tag:testing] and CategoryExample.

+++ Details

More details with bare CamelCase words.";

        var converter = CreateConverter();

        // Act
        var result = converter.ConvertContent(content);

        // Assert
        Assert.Contains("# Test Page", result);
        Assert.Contains("## Subsection", result);
        Assert.Contains("### Details", result);
        Assert.Contains("[[WikiLink]]", result);
        Assert.Contains("[[Another Link]]", result);
        Assert.Contains("and [[Already Done]].", result);
        Assert.Contains("with bare [[CamelCase]] words", result);
        Assert.Contains("#testing", result);
        Assert.Contains("#Example", result);
    }

    [Fact]
    public void ConvertLinks_DoesNotDoubleConvertAlreadyConverted()
    {
        // Arrange
        var content = "See [[WikiWord]] and [[Another Link]]";
        var converter = CreateConverter();

        // Act
        var result = converter.ConvertContent(content);

        // Assert
        Assert.Contains("[[WikiWord]]", result);
        Assert.Contains("[[Another Link]]", result);
        Assert.DoesNotContain("[[[", result); // No triple brackets
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
        var nonExistentPath = Path.Combine(_testFolder, "does_not_exist");

        // Act & Assert
        Assert.Throws<DirectoryNotFoundException>(() =>
            new WikidPadToObsidianConverter(nonExistentPath, _destDir));
    }

    [Fact]
    public void ConvertLinks_WikiLinksTestFile_ConvertsAllLinkTypes()
    {
        // Arrange
        var content = @"+ WikiLinks Test Page

++ Bare WikiWords (CamelCase)

WikiWord HomePage ProjectNotes MultipleWikiWords
What does it do for AbC, and ABc, or For ABC ?

++ Single Bracket Links

[Link with Spaces]
[Another Link]
[lowercase]
[123numbers]

++ Already Formatted Links

should not effect : [[Already Formatted]] .
should not effect : [[Another One]] ..

++ Mixed Content

[single bracket]
[[double bracket]]

++ Tags and Categories

Some text with [tag:example] and also a CategoryTest entry.
";

        var converter = CreateConverter();

        // Act
        var result = converter.ConvertContent(content);

        // Output for debugging
        Console.WriteLine("=== CONVERTED OUTPUT ===");
        Console.WriteLine(result);
        Console.WriteLine("=== END OUTPUT ===");

        // Assert - Headers (note: WikiLinks is converted to [[WikiLinks]])
        Assert.Contains("# [[WikiLinks]] Test Page", result);
        Assert.Contains("## Bare [[WikiWords]] ([[CamelCase]])", result);
        Assert.Contains("## Single Bracket Links", result);
        Assert.Contains("## Already Formatted Links", result);
        Assert.Contains("## Mixed Content", result);
        Assert.Contains("## Tags and Categories", result);

        // Assert - Bare WikiWords should be converted to [[WikiWord]]
        Assert.Contains("[[WikiWord]]", result);
        Assert.Contains("[[HomePage]]", result);
        Assert.Contains("[[ProjectNotes]]", result);
        Assert.Contains("[[MultipleWikiWords]]", result);

        // Assert - Single bracket links should be converted to [[double brackets]]
        Assert.Contains("[[Link with Spaces]]", result);
        Assert.Contains("[[Another Link]]", result);
        Assert.Contains("[[lowercase]]", result);
        Assert.Contains("[[123numbers]]", result);

        // Assert - Already formatted links should remain unchanged
        Assert.Contains("should not effect : [[Already Formatted]] .", result);
        Assert.Contains("should not effect : [[Another One]] ..", result);

        // Assert - Should NOT have any single bracket links remaining (except original input)
        // Count occurrences - the result should have fewer single brackets than input
        var inputSingleBrackets = System.Text.RegularExpressions.Regex.Matches(content, @"(?<!\[)\[(?!\[)").Count;
        var outputSingleBrackets = System.Text.RegularExpressions.Regex.Matches(result, @"(?<!\[)\[(?!\[)").Count;
        Assert.True(outputSingleBrackets < inputSingleBrackets,
            $"Expected fewer single brackets in output. Input: {inputSingleBrackets}, Output: {outputSingleBrackets}");

        // Assert - Already formatted links should remain unchanged
        Assert.Contains("[[Already Formatted]]", result);
        Assert.Contains("[[Another One]]", result);

        // Assert - Mixed content should all be converted
        Assert.Contains("[[single bracket]]", result);
        Assert.Contains("[[double bracket]]", result);

        // Assert - Tags should be converted
        Assert.Contains("#example", result);
        Assert.Contains("#Test", result);
    }

    [Fact]
    public void ConvertAttributes_SingleColon_ToDoubleColon()
    {
        // Arrange
        var content = "Some text [author: John Doe] more text";
        var converter = CreateConverter();

        // Act
        var result = converter.ConvertContent(content);

        // Assert
        Assert.Contains("[author:: John Doe]", result);
        Assert.DoesNotContain("[author: John Doe]", result);
    }

    [Fact]
    public void ConvertAttributes_MultipleAttributes_AllConverted()
    {
        // Arrange
        var content = "[author: John] [status: draft] [date: 2024-01-15]";
        var converter = CreateConverter();

        // Act
        var result = converter.ConvertContent(content);

        // Assert
        Assert.Contains("[author:: John]", result);
        Assert.Contains("[status:: draft]", result);
        Assert.Contains("[date:: 2024-01-15]", result);
    }

    [Fact]
    public void ConvertAttributes_WithHyphens_ConvertsCorrectly()
    {
        // Arrange
        var content = "[created-date: 2024-01-15] [author-name: John]";
        var converter = CreateConverter();

        // Act
        var result = converter.ConvertContent(content);

        // Assert
        Assert.Contains("[created-date:: 2024-01-15]", result);
        Assert.Contains("[author-name:: John]", result);
    }

    [Fact]
    public void ConvertAttributes_WithNumbers_ConvertsCorrectly()
    {
        // Arrange
        var content = "[version: 1.0.0] [build123: latest]";
        var converter = CreateConverter();

        // Act
        var result = converter.ConvertContent(content);

        // Assert
        Assert.Contains("[version:: 1.0.0]", result);
        Assert.Contains("[build123:: latest]", result);
    }

    [Fact]
    public void ConvertAttributes_LongValue_PreservesSpaces()
    {
        // Arrange
        var content = "[description: This is a long description with spaces]";
        var converter = CreateConverter();

        // Act
        var result = converter.ConvertContent(content);

        // Assert
        Assert.Contains("[description:: This is a long description with spaces]", result);
    }

    [Fact]
    public void ConvertContent_AttributesAndTagsAndLinks_AllConvertedInOrder()
    {
        // Arrange
        var content = @"+ Header
[tag:important]
[author: John]
Some WikiWord and [single link]
[status: draft]";
        var converter = CreateConverter();

        // Act
        var result = converter.ConvertContent(content);

        // Assert
        // Headers converted
        Assert.Contains("# Header", result);

        // Tags converted
        Assert.Contains("#important", result);

        // Attributes converted (not confused with tags)
        Assert.Contains("[author:: John]", result);
        Assert.Contains("[status:: draft]", result);

        // Links converted
        Assert.Contains("[[WikiWord]]", result);
        Assert.Contains("[[single link]]", result);
    }

    [Fact]
    public void ConvertAttributes_DoesNotConvertTags()
    {
        // Arrange - Tags should be converted by ConvertTags, not ConvertAttributes
        var content = "[tag:mytag]";
        var converter = CreateConverter();

        // Act
        var result = converter.ConvertContent(content);

        // Assert
        // Tag should be converted to # format, not [tag:: format]
        Assert.Contains("#mytag", result);
        Assert.DoesNotContain("[tag::", result);
    }

    private WikidPadToObsidianConverter CreateConverter()
    {
        Directory.CreateDirectory(_sourceDir);
        return new WikidPadToObsidianConverter(_sourceDir, _destDir);
    }
}
