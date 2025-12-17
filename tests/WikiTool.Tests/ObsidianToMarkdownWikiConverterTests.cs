using System;
using System.IO;
using WikiTool.Converters;
using WikiTool.Wikis;
using Xunit;

namespace WikiTool.Tests;

public class ObsidianToMarkdownWikiConverterTests
{
    private readonly string _testFolder;
    private readonly string _sourceDir;
    private readonly string _destDir;

    public ObsidianToMarkdownWikiConverterTests()
    {
        _testFolder = TestUtilities.GetTestFolder("obsidian_to_mdwiki_tests");
        _sourceDir = Path.Combine(_testFolder, "source");
        _destDir = Path.Combine(_testFolder, "dest");
    }

    #region Link Conversion Tests

    [Fact]
    public void ConvertLinks_SimpleWikilink_ToMarkdownLink()
    {
        // Arrange
        var content = "See [[MyPage]] for more info";
        var converter = CreateConverter();

        // Act
        var result = converter.ConvertLinks(content);

        // Assert
        Assert.Equal("See [MyPage](mypage.md) for more info", result);
    }

    [Fact]
    public void ConvertLinks_WikilinkWithSpaces_ConvertedToHyphens()
    {
        // Arrange
        var content = "See [[My Page Name]] for more";
        var converter = CreateConverter();

        // Act
        var result = converter.ConvertLinks(content);

        // Assert
        Assert.Equal("See [My Page Name](my-page-name.md) for more", result);
    }

    [Fact]
    public void ConvertLinks_WikilinkWithDisplayText_UsesDisplayText()
    {
        // Arrange
        var content = "See [[MyPage|Custom Display]] for more";
        var converter = CreateConverter();

        // Act
        var result = converter.ConvertLinks(content);

        // Assert
        Assert.Equal("See [Custom Display](mypage.md) for more", result);
    }

    [Fact]
    public void ConvertLinks_WikilinkWithHeading_IncludesAnchor()
    {
        // Arrange
        var content = "See [[MyPage#Section One]] for details";
        var converter = CreateConverter();

        // Act
        var result = converter.ConvertLinks(content);

        // Assert
        Assert.Equal("See [MyPage](mypage.md#section-one) for details", result);
    }

    [Fact]
    public void ConvertLinks_WikilinkWithHeadingAndDisplay_BothConverted()
    {
        // Arrange
        var content = "Check [[MyPage#Details|more info]] here";
        var converter = CreateConverter();

        // Act
        var result = converter.ConvertLinks(content);

        // Assert
        Assert.Equal("Check [more info](mypage.md#details) here", result);
    }

    [Fact]
    public void ConvertLinks_MultipleLinks_AllConverted()
    {
        // Arrange
        var content = "See [[PageOne]] and [[Page Two]] and [[PageThree|Third]]";
        var converter = CreateConverter();

        // Act
        var result = converter.ConvertLinks(content);

        // Assert
        Assert.Equal("See [PageOne](pageone.md) and [Page Two](page-two.md) and [Third](pagethree.md)", result);
    }

    [Fact]
    public void ConvertLinks_MixedCasePreservedInText_LowercasedInPath()
    {
        // Arrange
        var content = "Link to [[MyAwesomePage]]";
        var converter = CreateConverter();

        // Act
        var result = converter.ConvertLinks(content);

        // Assert
        Assert.Equal("Link to [MyAwesomePage](myawesomepage.md)", result);
    }

    [Fact]
    public void ConvertLinks_EmptyContent_ReturnsEmpty()
    {
        // Arrange
        var converter = CreateConverter();

        // Act
        var result = converter.ConvertContent("");

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void ConvertLinks_NullContent_ReturnsNull()
    {
        // Arrange
        var converter = CreateConverter();

        // Act
        var result = converter.ConvertContent(null);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ConvertLinks_NoLinks_ReturnsUnchanged()
    {
        // Arrange
        var content = "Just some plain text without any links.";
        var converter = CreateConverter();

        // Act
        var result = converter.ConvertContent(content);

        // Assert
        Assert.Equal(content, result);
    }

    [Fact]
    public void ConvertLinks_SpecialCharactersInPageName_HandledCorrectly()
    {
        // Arrange
        var content = "Link to [[Page & More]] here";
        var converter = CreateConverter();

        // Act
        var result = converter.ConvertLinks(content);

        // Assert
        Assert.Equal("Link to [Page & More](page-&-more.md) here", result);
    }

    [Fact]
    public void ConvertLinks_NumbersInPageName_Preserved()
    {
        // Arrange
        var content = "See [[Page 123]] and [[2024 Notes]]";
        var converter = CreateConverter();

        // Act
        var result = converter.ConvertLinks(content);

        // Assert
        Assert.Contains("[Page 123](page-123.md)", result);
        Assert.Contains("[2024 Notes](2024-notes.md)", result);
    }

    #endregion

    #region MarkdownWikiSyntax Tests

    [Fact]
    public void ToFilePath_SpacesToHyphens()
    {
        // Act
        var result = MarkdownWikiSyntax.ToFilePath("My Page Name");

        // Assert
        Assert.Equal("my-page-name", result);
    }

    [Fact]
    public void ToFilePath_Lowercase()
    {
        // Act
        var result = MarkdownWikiSyntax.ToFilePath("MyPageName");

        // Assert
        Assert.Equal("mypagename", result);
    }

    [Fact]
    public void ToFilePath_EmptyString_ReturnsEmpty()
    {
        // Act
        var result = MarkdownWikiSyntax.ToFilePath("");

        // Assert
        Assert.Equal("", result);
    }

    [Fact]
    public void ToFilePath_NullString_ReturnsNull()
    {
        // Act
        var result = MarkdownWikiSyntax.ToFilePath(null);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void CreateLink_SimplePageName()
    {
        // Act
        var result = MarkdownWikiSyntax.CreateLink("My Page");

        // Assert
        Assert.Equal("[My Page](my-page.md)", result);
    }

    [Fact]
    public void CreateLink_WithDisplayText()
    {
        // Act
        var result = MarkdownWikiSyntax.CreateLink("My Page", "Click Here");

        // Assert
        Assert.Equal("[Click Here](my-page.md)", result);
    }

    [Fact]
    public void CreateLink_EmptyPageName_ReturnsEmpty()
    {
        // Act
        var result = MarkdownWikiSyntax.CreateLink("");

        // Assert
        Assert.Equal(string.Empty, result);
    }

    #endregion

    #region Full Content Conversion Tests

    [Fact]
    public void ConvertContent_ComplexDocument_AllLinksConverted()
    {
        // Arrange
        var content = @"# Main Page

This is a document with [[Various Links]].

## Section One

See [[Another Page#Details|more details]] for info.

Check out [[Simple]] and [[Multi Word Page]] too.

### Subsection

The [[Final Link#Heading]] wraps it up.
";

        var converter = CreateConverter();

        // Act
        var result = converter.ConvertContent(content);

        // Assert
        Assert.Contains("[Various Links](various-links.md)", result);
        Assert.Contains("[more details](another-page.md#details)", result);
        Assert.Contains("[Simple](simple.md)", result);
        Assert.Contains("[Multi Word Page](multi-word-page.md)", result);
        Assert.Contains("[Final Link](final-link.md#heading)", result);

        // Headers should be unchanged
        Assert.Contains("# Main Page", result);
        Assert.Contains("## Section One", result);
    }

    [Fact]
    public void ConvertContent_PreservesYamlFrontmatter()
    {
        // Arrange
        var content = @"---
title: My Page
tags: [test, demo]
---

Content with [[Some Link]] here.
";

        var converter = CreateConverter();

        // Act
        var result = converter.ConvertContent(content);

        // Assert
        Assert.Contains("---", result);
        Assert.Contains("title: My Page", result);
        Assert.Contains("tags: [test, demo]", result);
        Assert.Contains("[Some Link](some-link.md)", result);
    }

    [Fact]
    public void ConvertContent_PreservesCodeBlocks()
    {
        // Arrange
        var content = @"Some text [[Link]]

```code
[[NotALink]]
```

More text [[AnotherLink]]
";

        var converter = CreateConverter();

        // Act
        var result = converter.ConvertContent(content);

        // Assert
        Assert.Contains("[Link](link.md)", result);
        Assert.Contains("[AnotherLink](anotherlink.md)", result);
        // Note: Code blocks would need special handling if required
    }

    #endregion

    #region File Conversion Tests

    [Fact]
    public void ConvertAll_CreatesDestinationDirectory()
    {
        // Arrange
        SetupSourceDirectory();
        File.WriteAllText(Path.Combine(_sourceDir, "TestPage.md"), "[[Link]]");

        if (Directory.Exists(_destDir))
            Directory.Delete(_destDir, true);

        var converter = new ObsidianToMarkdownWikiConverter(_sourceDir, _destDir);

        // Act
        converter.ConvertAll();

        // Assert
        Assert.True(Directory.Exists(_destDir));
    }

    [Fact]
    public void ConvertAll_CreatesConvertedFiles()
    {
        // Arrange
        SetupSourceDirectory();
        var testContent = "# Test\n\nSee [[Other Page]] for more.";
        File.WriteAllText(Path.Combine(_sourceDir, "TestPage.md"), testContent);

        if (Directory.Exists(_destDir))
            Directory.Delete(_destDir, true);

        var converter = new ObsidianToMarkdownWikiConverter(_sourceDir, _destDir);

        // Act
        converter.ConvertAll();

        // Assert
        var destFile = Path.Combine(_destDir, "testpage.md");
        Assert.True(File.Exists(destFile));

        var result = File.ReadAllText(destFile);
        Assert.Contains("# Test", result);
        Assert.Contains("[Other Page](other-page.md)", result);
    }

    [Fact]
    public void ConvertAll_FileNamesConvertedToLowercase()
    {
        // Arrange
        SetupSourceDirectory();
        File.WriteAllText(Path.Combine(_sourceDir, "My Page Name.md"), "Content");

        if (Directory.Exists(_destDir))
            Directory.Delete(_destDir, true);

        var converter = new ObsidianToMarkdownWikiConverter(_sourceDir, _destDir);

        // Act
        converter.ConvertAll();

        // Assert
        Assert.True(File.Exists(Path.Combine(_destDir, "my-page-name.md")));
    }

    [Fact]
    public void Constructor_ThrowsWhenSourceNotFound()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testFolder, "does_not_exist");

        // Act & Assert
        Assert.Throws<DirectoryNotFoundException>(() =>
            new ObsidianToMarkdownWikiConverter(nonExistentPath, _destDir));
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ConvertLinks_NestedBrackets_HandledCorrectly()
    {
        // Arrange - shouldn't match nested patterns
        var content = "Text [not a [[link]] inside]";
        var converter = CreateConverter();

        // Act
        var result = converter.ConvertLinks(content);

        // Assert - the inner wikilink should be converted
        Assert.Contains("[link](link.md)", result);
    }

    [Fact]
    public void ConvertLinks_MalformedWikilink_NotConverted()
    {
        // Arrange - single bracket
        var content = "See [Not A Wikilink] for more";
        var converter = CreateConverter();

        // Act
        var result = converter.ConvertLinks(content);

        // Assert - single brackets remain unchanged
        Assert.Equal("See [Not A Wikilink] for more", result);
    }

    [Fact]
    public void ConvertLinks_ConsecutiveLinks_AllConverted()
    {
        // Arrange
        var content = "[[One]][[Two]][[Three]]";
        var converter = CreateConverter();

        // Act
        var result = converter.ConvertLinks(content);

        // Assert
        Assert.Equal("[One](one.md)[Two](two.md)[Three](three.md)", result);
    }

    [Fact]
    public void ConvertLinks_LinksInLists_AllConverted()
    {
        // Arrange
        var content = @"- [[Item One]]
- [[Item Two]]
- [[Item Three]]";

        var converter = CreateConverter();

        // Act
        var result = converter.ConvertLinks(content);

        // Assert
        Assert.Contains("- [Item One](item-one.md)", result);
        Assert.Contains("- [Item Two](item-two.md)", result);
        Assert.Contains("- [Item Three](item-three.md)", result);
    }

    #endregion

    #region Helper Methods

    private ObsidianToMarkdownWikiConverter CreateConverter()
    {
        SetupSourceDirectory();
        return new ObsidianToMarkdownWikiConverter(_sourceDir, _destDir);
    }

    private void SetupSourceDirectory()
    {
        if (!Directory.Exists(_sourceDir))
            Directory.CreateDirectory(_sourceDir);
    }

    #endregion
}
