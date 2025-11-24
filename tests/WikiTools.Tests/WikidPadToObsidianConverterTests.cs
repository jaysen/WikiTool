using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
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
    public void ConvertTags_CategoryFormat_NotConvertedByDefault()
    {
        // Arrange
        var content = "CategoryTests and more content";
        var converter = CreateConverter();

        // Act
        var result = converter.ConvertContent(content);

        // Assert - Category tags are NOT converted by default
        Assert.Contains("CategoryTests", result);
        Assert.DoesNotContain("#Tests", result);
    }

    [Fact]
    public void ConvertTags_CategoryFormat_ConvertedWhenEnabled()
    {
        // Arrange
        var content = "CategoryTests and more content";
        var converter = CreateConverter();
        converter.ConvertCategoryTags = true;

        // Act
        var result = converter.ConvertContent(content);

        // Assert
        Assert.Contains("#Tests", result);
        Assert.DoesNotContain("CategoryTests", result);
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
        converter.ConvertCategoryTags = true;

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
    public void ConvertLinks_LowercaseCamelCase_NotConverted()
    {
        // Arrange - lowercase-starting CamelCase should NOT be converted (issue #3)
        var content = "Test aaBB and aaBbbCcc and camelCase and iPhone here";
        var converter = CreateConverter();

        // Act
        var result = converter.ConvertContent(content);

        // Assert - these should remain unchanged (not converted to links)
        Assert.Contains("aaBB", result);
        Assert.Contains("aaBbbCcc", result);
        Assert.Contains("camelCase", result);
        Assert.Contains("iPhone", result);
        Assert.DoesNotContain("[[aaBB]]", result);
        Assert.DoesNotContain("[[aaBbbCcc]]", result);
        Assert.DoesNotContain("[[camelCase]]", result);
        Assert.DoesNotContain("[[iPhone]]", result);
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
        converter.ConvertCategoryTags = true;

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
        converter.ConvertCategoryTags = true;

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

    [Fact]
    public void ConvertContent_IgnoresEqualSignAttributes()
    {
        // Arrange - WikidPad special attributes like [icon=date] use = instead of :
        // These should be left unchanged (not converted to Obsidian attributes or links)
        var content = "[icon=date] [icon=pin] [color=blue]";
        var converter = CreateConverter();

        // Act
        var result = converter.ConvertContent(content);

        // Assert
        // Equal sign attributes should remain unchanged
        Assert.Contains("[icon=date]", result);
        Assert.Contains("[icon=pin]", result);
        Assert.Contains("[color=blue]", result);
        // Should NOT be converted to links
        Assert.DoesNotContain("[[icon=date]]", result);
        Assert.DoesNotContain("[[icon=pin]]", result);
        Assert.DoesNotContain("[[color=blue]]", result);
    }

    [Fact]
    public void ConvertContent_Attributes_DoesNotConvertToLinks()
    {
        // Arrange
        var content = @"+ These are attributes
[tag:important]
[author::John] [key:value] [keyTwo:: valueTwo]
Some WikiWord and [single link]
[status: draft]
";

        var converter = CreateConverter();

        // Act
        var result = converter.ConvertContent(content);

        // Assert
        // attributes should not be converted to links
        Assert.Contains("[author::John]", result);
        Assert.Contains("[key:: value]", result);
        Assert.Contains("[keyTwo:: valueTwo]", result);
        Assert.DoesNotContain("[[author::John]]", result);
        Assert.DoesNotContain("[[key::value]]", result);
        Assert.DoesNotContain("[[key:: value]]", result);
        Assert.DoesNotContain("[[keyTwo::", result);
        Assert.DoesNotContain("[[status:: draft]]", result);
    }

    #region Alias Conversion Tests

    [Fact]
    public void ConvertAliases_SingleAlias_GeneratesYamlFrontmatter()
    {
        // Arrange
        var content = "[alias:MyAlias]\n+ Header\nSome content";
        var converter = CreateConverter();

        // Act
        var result = converter.ConvertContent(content);

        // Assert
        Assert.StartsWith("---", result);
        Assert.Contains("aliases:", result);
        Assert.Contains("  - MyAlias", result);
        Assert.Contains("# Header", result);
    }

    [Fact]
    public void ConvertAliases_MultipleAliases_AllInFrontmatter()
    {
        // Arrange
        var content = "[alias:FirstAlias] [alias:SecondAlias]\n+ Header";
        var converter = CreateConverter();

        // Act
        var result = converter.ConvertContent(content);

        // Assert
        Assert.StartsWith("---", result);
        Assert.Contains("aliases:", result);
        Assert.Contains("  - FirstAlias", result);
        Assert.Contains("  - SecondAlias", result);
    }

    [Fact]
    public void ConvertAliases_AliasWithSpaces_PreservedInFrontmatter()
    {
        // Arrange
        var content = "[alias:My Alias Name]\nContent";
        var converter = CreateConverter();

        // Act
        var result = converter.ConvertContent(content);

        // Assert
        Assert.Contains("  - My Alias Name", result);
    }


    [Fact]
    public void ConvertAliases_RemovesAliasTagsFromContent()
    {
        // Arrange
        var content = "[alias:MyAlias]\n+ Header\nSome content";
        var converter = CreateConverter();

        // Act
        var result = converter.ConvertContent(content);

        // Assert
        Assert.DoesNotContain("[alias:", result);
    }

    [Fact]
    public void ConvertAliases_CompleteConversion_CorrectFormat()
    {
        // Arrange - exact format from issue #5
        var content = "[alias:FirstAlias] [alias:SecondAlias]\n+ My Page\nContent here";
        var converter = CreateConverter();

        // Act
        var result = converter.ConvertContent(content);

        // Assert - verify exact YAML format
        var lines = result.Split('\n');
        Assert.Equal("---", lines[0]);
        Assert.Equal("aliases:", lines[1]);
        Assert.Equal("  - FirstAlias", lines[2]);
        Assert.Equal("  - SecondAlias", lines[3]);
        Assert.Equal("---", lines[4]);
        Assert.Contains("# My Page", result);
    }

    [Fact]
    public void ConvertAliases_WithOtherElements_AllConverted()
    {
        // Arrange
        var content = @"[alias:PageAlias]
+ Test Page
[tag:important]
[author: John]
Some WikiWord here";
        var converter = CreateConverter();

        // Act
        var result = converter.ConvertContent(content);

        // Assert
        // YAML frontmatter with alias
        Assert.StartsWith("---", result);
        Assert.Contains("  - PageAlias", result);
        // Other conversions still work
        Assert.Contains("# Test Page", result);
        Assert.Contains("#important", result);
        Assert.Contains("[author:: John]", result);
        Assert.Contains("[[WikiWord]]", result);
        // Alias tag removed from content
        Assert.DoesNotContain("[alias:", result);
    }

    [Fact]
    public void ConvertAliases_WithExistingFrontmatter_MergesIntoExisting()
    {
        // Arrange - content already has frontmatter with other properties
        var content = @"---
title: My Page
tags: [important]
---
[alias:PageAlias]
+ Header
Content here";
        var converter = CreateConverter();

        // Act
        var result = converter.ConvertContent(content);

        // Assert - should have single frontmatter block with all properties
        var frontmatterCount = Regex.Matches(result, "^---$", RegexOptions.Multiline).Count;
        Assert.Equal(2, frontmatterCount); // Only one opening and one closing ---

        Assert.Contains("title: My Page", result);
        Assert.Contains("tags: [important]", result);
        Assert.Contains("aliases:", result);
        Assert.Contains("  - PageAlias", result);
        Assert.DoesNotContain("[alias:", result);
    }

    [Fact]
    public void ConvertAliases_WithExistingAliasesInFrontmatter_AppendsToExisting()
    {
        // Arrange - content already has aliases in frontmatter
        var content = @"---
aliases:
  - ExistingAlias
---
[alias:NewAlias]
+ Header
Content here";
        var converter = CreateConverter();

        // Act
        var result = converter.ConvertContent(content);

        // Assert - should preserve existing alias and add new one
        var frontmatterCount = Regex.Matches(result, "^---$", RegexOptions.Multiline).Count;
        Assert.Equal(2, frontmatterCount);

        Assert.Contains("  - ExistingAlias", result);
        Assert.Contains("  - NewAlias", result);
        Assert.DoesNotContain("[alias:", result);
    }

    [Fact]
    public void ConvertAliases_WithExistingAliasesAndOtherProperties_MaintainsOrder()
    {
        // Arrange - frontmatter has aliases in middle of other properties
        var content = @"---
title: Test
aliases:
  - FirstAlias
author: John
---
[alias:SecondAlias]
+ Header";
        var converter = CreateConverter();

        // Act
        var result = converter.ConvertContent(content);

        // Assert - should preserve structure and add new alias to aliases section
        Assert.Contains("title: Test", result);
        Assert.Contains("aliases:", result);
        Assert.Contains("  - FirstAlias", result);
        Assert.Contains("  - SecondAlias", result);
        Assert.Contains("author: John", result);

        // Verify aliases section is not duplicated
        var aliasesKeyCount = Regex.Matches(result, "^aliases:", RegexOptions.Multiline).Count;
        Assert.Equal(1, aliasesKeyCount);
    }

    [Fact]
    public void ConvertAliases_WithNoExistingFrontmatter_CreatesNew()
    {
        // Arrange - no existing frontmatter (same as existing test but explicit)
        var content = "[alias:MyAlias]\n+ Header\nContent";
        var converter = CreateConverter();

        // Act
        var result = converter.ConvertContent(content);

        // Assert - should create frontmatter from scratch
        Assert.StartsWith("---", result);
        Assert.Contains("aliases:", result);
        Assert.Contains("  - MyAlias", result);
        var frontmatterCount = Regex.Matches(result, "^---$", RegexOptions.Multiline).Count;
        Assert.Equal(2, frontmatterCount);
    }

    [Fact]
    public void ConvertAliases_WithMultipleAliasesAndExistingFrontmatter_AllMerged()
    {
        // Arrange
        var content = @"---
title: Test Page
---
[alias:FirstAlias] [alias:SecondAlias] [alias:ThirdAlias]
+ Header";
        var converter = CreateConverter();

        // Act
        var result = converter.ConvertContent(content);

        // Assert
        Assert.Contains("title: Test Page", result);
        Assert.Contains("aliases:", result);
        Assert.Contains("  - FirstAlias", result);
        Assert.Contains("  - SecondAlias", result);
        Assert.Contains("  - ThirdAlias", result);

        // Single frontmatter block
        var frontmatterCount = Regex.Matches(result, "^---$", RegexOptions.Multiline).Count;
        Assert.Equal(2, frontmatterCount);
    }

    [Fact]
    public void ConvertAliases_WithComplexFrontmatter_PreservesStructure()
    {
        // Arrange - complex frontmatter with nested structures
        var content = @"---
title: Complex Page
tags: [tag1, tag2]
metadata:
  author: John Doe
  date: 2024-01-15
---
[alias:ComplexAlias]
+ Content";
        var converter = CreateConverter();

        // Act
        var result = converter.ConvertContent(content);

        // Assert - all existing properties preserved
        Assert.Contains("title: Complex Page", result);
        Assert.Contains("tags: [tag1, tag2]", result);
        Assert.Contains("metadata:", result);
        Assert.Contains("  author: John Doe", result);
        Assert.Contains("  date: 2024-01-15", result);
        Assert.Contains("aliases:", result);
        Assert.Contains("  - ComplexAlias", result);
    }

    [Fact]
    public void ConvertAll_WithFrontmatterAndAliases_ConvertsCorrectly()
    {
        // Arrange - Create a real file with frontmatter and aliases
        Directory.CreateDirectory(_sourceDir);
        var dataDir = Path.Combine(_sourceDir, "data");
        Directory.CreateDirectory(dataDir);

        var testContent = @"---
title: My Wiki Page
author: John Doe
tags: [documentation, reference]
created: 2024-01-15
---
[alias:MyPageAlias] [alias:SecondAlias]

+ Introduction

This is a test page with existing frontmatter and aliases.

++ Features

The page demonstrates:
- Existing frontmatter properties
- Multiple alias tags
- [tag:important] content
- Links to OtherPage and [Another Page]
- [status: draft] attribute

++ CamelCase Links

Some WikiWord links and ProjectNotes references.

+++ Details

More detailed information with [priority: high] attribute.
";

        File.WriteAllText(Path.Combine(dataDir, "PageWithFrontmatter.wiki"), testContent);

        if (Directory.Exists(_destDir))
        {
            Directory.Delete(_destDir, true);
        }

        var converter = new WikidPadToObsidianConverter(_sourceDir, _destDir);

        // Act
        converter.ConvertAll();

        // Assert
        var mdFile = Path.Combine(_destDir, "PageWithFrontmatter.md");
        Assert.True(File.Exists(mdFile), "Converted markdown file should exist");

        var result = File.ReadAllText(mdFile);

        // Verify single frontmatter block
        var frontmatterCount = Regex.Matches(result, "^---$", RegexOptions.Multiline).Count;
        Assert.Equal(2, frontmatterCount);

        // Verify original frontmatter properties preserved
        Assert.Contains("title: My Wiki Page", result);
        Assert.Contains("author: John Doe", result);
        Assert.Contains("tags: [documentation, reference]", result);
        Assert.Contains("created: 2024-01-15", result);

        // Verify aliases merged into frontmatter
        Assert.Contains("aliases:", result);
        Assert.Contains("  - MyPageAlias", result);
        Assert.Contains("  - SecondAlias", result);

        // Verify [alias:...] tags removed from content
        Assert.DoesNotContain("[alias:", result);

        // Verify headers converted
        Assert.Contains("# Introduction", result);
        Assert.Contains("## Features", result);
        Assert.Contains("## [[CamelCase]] Links", result); // CamelCase gets converted to link
        Assert.Contains("### Details", result);

        // Verify tags converted
        Assert.Contains("#important", result);
        Assert.DoesNotContain("[tag:", result);

        // Verify attributes converted
        Assert.Contains("[status:: draft]", result);
        Assert.Contains("[priority:: high]", result);
        Assert.DoesNotContain("[status: draft]", result);
        Assert.DoesNotContain("[priority: high]", result);

        // Verify links converted
        Assert.Contains("[[OtherPage]]", result);
        Assert.Contains("[[Another Page]]", result);
        Assert.Contains("[[WikiWord]]", result);
        Assert.Contains("[[ProjectNotes]]", result);

        // Verify structure: frontmatter at top, then content
        Assert.StartsWith("---", result);
        var contentAfterFrontmatter = result.Substring(result.IndexOf("---", 4) + 4);
        Assert.Contains("# Introduction", contentAfterFrontmatter);
    }

    #endregion

    #region Missing Closing Bracket Tests

    [Fact]
    public void ConvertLinks_MissingClosingBracket_HandledSafely()
    {
        // Arrange - Create test files
        Directory.CreateDirectory(_sourceDir);
        var dataDir = Path.Combine(_sourceDir, "data");
        Directory.CreateDirectory(dataDir);

        var sourceContent = @"+ Test Page with Malformed Links

This is a link with missing bracket: [MissingBracket

And here's a valid link: [ValidLink]

Another malformed: [AnotherMissing at the end of file";

        var sourceFile = Path.Combine(dataDir, "MalformedLinks.wiki");
        File.WriteAllText(sourceFile, sourceContent);

        if (Directory.Exists(_destDir))
        {
            Directory.Delete(_destDir, true);
        }

        var converter = new WikidPadToObsidianConverter(_sourceDir, _destDir);

        // Act
        converter.ConvertAll();

        // Assert
        var destFile = Path.Combine(_destDir, "MalformedLinks.md");
        Assert.True(File.Exists(destFile), "Output file should be created");

        var result = File.ReadAllText(destFile);

        // Valid link should be converted
        Assert.Contains("[[ValidLink]]", result);

        // Malformed links should be left as-is (not converted)
        Assert.Contains("[MissingBracket", result);
        Assert.Contains("[AnotherMissing", result);

        // Should not create malformed output like [[MissingBracket
        Assert.DoesNotContain("[[MissingBracket\n", result);
    }

    [Fact]
    public void ConvertTags_MissingClosingBracket_HandledSafely()
    {
        // Arrange - Create test files
        Directory.CreateDirectory(_sourceDir);
        var dataDir = Path.Combine(_sourceDir, "data");
        Directory.CreateDirectory(dataDir);

        var sourceContent = @"+ Test Page with Malformed Tags

This is a tag with missing bracket: [tag:important

And here's a valid tag: [tag:valid]

Another malformed: [tag:another at the end";

        var sourceFile = Path.Combine(dataDir, "MalformedTags.wiki");
        File.WriteAllText(sourceFile, sourceContent);

        if (Directory.Exists(_destDir))
        {
            Directory.Delete(_destDir, true);
        }

        var converter = new WikidPadToObsidianConverter(_sourceDir, _destDir);

        // Act
        converter.ConvertAll();

        // Assert
        var destFile = Path.Combine(_destDir, "MalformedTags.md");
        Assert.True(File.Exists(destFile), "Output file should be created");

        var result = File.ReadAllText(destFile);

        // Valid tag should be converted
        Assert.Contains("#valid", result);

        // Malformed tags should be left as-is (not converted)
        Assert.Contains("[tag:important", result);
        Assert.Contains("[tag:another", result);

        // Should not create malformed output
        Assert.DoesNotContain("#important\n", result.Replace("#valid", "")); // Avoid matching valid tag
    }

    [Fact]
    public void ConvertAttributes_MissingClosingBracket_HandledSafely()
    {
        // Arrange - Create test files
        Directory.CreateDirectory(_sourceDir);
        var dataDir = Path.Combine(_sourceDir, "data");
        Directory.CreateDirectory(dataDir);

        var sourceContent = @"+ Test Page with Malformed Attributes

This is an attribute with missing bracket: [author: John Doe

And here's a valid attribute: [status: draft]

Another malformed: [date: 2024-01-15 at the end";

        var sourceFile = Path.Combine(dataDir, "MalformedAttributes.wiki");
        File.WriteAllText(sourceFile, sourceContent);

        if (Directory.Exists(_destDir))
        {
            Directory.Delete(_destDir, true);
        }

        var converter = new WikidPadToObsidianConverter(_sourceDir, _destDir);

        // Act
        converter.ConvertAll();

        // Assert
        var destFile = Path.Combine(_destDir, "MalformedAttributes.md");
        Assert.True(File.Exists(destFile), "Output file should be created");

        var result = File.ReadAllText(destFile);

        // Valid attribute should be converted
        Assert.Contains("[status:: draft]", result);

        // Malformed attributes should be left as-is (not converted)
        Assert.Contains("[author: John Doe", result);
        Assert.Contains("[date: 2024-01-15", result);

        // Should not create malformed output
        Assert.DoesNotContain("[author:: John Doe\n", result);
    }

    [Fact]
    public void ConvertAliases_MissingClosingBracket_HandledSafely()
    {
        // Arrange - Create test files
        Directory.CreateDirectory(_sourceDir);
        var dataDir = Path.Combine(_sourceDir, "data");
        Directory.CreateDirectory(dataDir);

        var sourceContent = @"[alias:ValidAlias]
[alias:missingbracket
+ Test Page

Some content here";

        var sourceFile = Path.Combine(dataDir, "MalformedAliases.wiki");
        File.WriteAllText(sourceFile, sourceContent);

        if (Directory.Exists(_destDir))
        {
            Directory.Delete(_destDir, true);
        }

        var converter = new WikidPadToObsidianConverter(_sourceDir, _destDir);

        // Act
        converter.ConvertAll();

        // Assert
        var destFile = Path.Combine(_destDir, "MalformedAliases.md");
        Assert.True(File.Exists(destFile), "Output file should be created");

        var result = File.ReadAllText(destFile);

        // Valid alias should be in frontmatter
        Assert.Contains("---", result);
        Assert.Contains("aliases:", result);
        Assert.Contains("  - ValidAlias", result);

        // Malformed alias should be left in content (not in frontmatter)
        // Note: 'missingbracket' is not CamelCase so won't be converted to a link
        Assert.Contains("[alias:missingbracket", result);

        // Frontmatter should NOT contain the malformed alias
        var lines = result.Split('\n');
        var frontmatterEnd = Array.IndexOf(lines, "---", 1); // Find second ---
        var frontmatter = string.Join('\n', lines.Take(frontmatterEnd + 1));
        Assert.DoesNotContain("missingbracket", frontmatter);
    }

    [Fact]
    public void ConvertContent_MixedMalformedPatterns_HandlesAllSafely()
    {
        // Arrange - Create test files with multiple types of malformed patterns
        Directory.CreateDirectory(_sourceDir);
        var dataDir = Path.Combine(_sourceDir, "data");
        Directory.CreateDirectory(dataDir);

        var sourceContent = @"+ Mixed Malformed Patterns

[tag:malformed-tag
[author: incomplete-attr
[alias:broken-alias

Valid patterns:
[tag:good-tag]
[status: complete]
[ValidLink]

More malformed at end: [another-link and [key: value";

        var sourceFile = Path.Combine(dataDir, "MixedMalformed.wiki");
        File.WriteAllText(sourceFile, sourceContent);

        if (Directory.Exists(_destDir))
        {
            Directory.Delete(_destDir, true);
        }

        var converter = new WikidPadToObsidianConverter(_sourceDir, _destDir);

        // Act
        converter.ConvertAll();

        // Assert
        var destFile = Path.Combine(_destDir, "MixedMalformed.md");
        Assert.True(File.Exists(destFile), "Output file should be created");

        var result = File.ReadAllText(destFile);

        // Valid patterns should be converted
        Assert.Contains("#good-tag", result);
        Assert.Contains("[status:: complete]", result);
        Assert.Contains("[[ValidLink]]", result);

        // Malformed patterns should remain unchanged
        Assert.Contains("[tag:malformed-tag", result);
        Assert.Contains("[author: incomplete-attr", result);
        Assert.Contains("[alias:broken-alias", result);
        Assert.Contains("[another-link", result);
        Assert.Contains("[key: value", result);
    }

    [Fact]
    public void ConvertContent_MalformedPatternsInline_HandledGracefully()
    {
        // Arrange
        // Note: When malformed patterns appear on the same line as valid ones,
        // the regex may greedily match to the valid pattern's closing bracket.
        // The newline exclusion prevents cross-line issues, but same-line overlaps
        // are inherent to regex limitations.
        var content = "Before [tag:broken\ntext after and [link\nhere [status: done] end.";
        var converter = CreateConverter();

        // Act
        var result = converter.ConvertContent(content);

        // Assert
        // Valid attribute should be converted
        Assert.Contains("[status:: done]", result);

        // Malformed patterns on different lines should remain unchanged
        Assert.Contains("[tag:broken", result);
        Assert.Contains("[link", result);

        // Surrounding text should be intact
        Assert.Contains("Before", result);
        Assert.Contains("text after", result);
        Assert.Contains("here", result);
        Assert.Contains("end.", result);
    }

    #endregion

    private WikidPadToObsidianConverter CreateConverter()
    {
        Directory.CreateDirectory(_sourceDir);
        return new WikidPadToObsidianConverter(_sourceDir, _destDir);
    }
}
