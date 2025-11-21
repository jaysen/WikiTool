using System;
using System.IO;
using WikiTools;
using Xunit;

namespace WikiTools.Tests;

public class WikidpadPageTests
{
    private readonly string _testFolder;

    public WikidpadPageTests()
    {
        _testFolder = TestUtilities.GetTestFolder("wikidpad_page_tests");
    }

    [Fact]
    public void Constructor_ThrowsIfFileNotExists()
    {
        //arrange
        var path = "NoSuchFile.wiki";

        //assert
        Assert.Throws<FileNotFoundException>(() => new WikidpadPage(path));
    }

    [Fact]
    public void Constructor_ThrowsIfWrongExtension()
    {
        //arrange
        var path = Path.Combine(_testFolder, "wrong_extension_test.md");
        File.Create(path);

        //assert
        Assert.Throws<FormatException>( () => new WikidpadPage(path));
    }

    [Fact]
    public void GetPageContent_ReadSimpleText()
    {
        //arrange
        var path = Path.Combine(_testFolder, "simple_read_test.wiki");
        var expected = "test1";
        File.WriteAllText(path, expected);

        //actual
        var sut = new WikidpadPage(path);
        var actual = LocalPages.GetPageContent(path);

        //assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GetPageContent_ThrowsIfFileNotExists()
    {
        //arrange
        var path = "NoFileOfThisNameExists.txt";
        //assert
        Assert.Throws<FileNotFoundException>(() => new WikidpadPage(path));
    }

    [Fact]
    public void GetAttributes_ParsesBasicAttribute()
    {
        // Arrange
        var path = Path.Combine(_testFolder, "attributes_basic_test.wiki");
        var content = "Some text [author: John Doe] more text";
        File.WriteAllText(path, content);

        // Act
        var wiki = new WikidpadWiki(_testFolder);
        var sut = new WikidpadPage(path, wiki);
        var attributes = sut.GetAttributes();

        // Assert
        Assert.Equal(1, attributes.Count);
        Assert.True(attributes.ContainsKey("author"));
        Assert.Equal("John Doe", attributes["author"]);
    }

    [Fact]
    public void GetAttributes_ParsesMultipleAttributes()
    {
        // Arrange
        var path = Path.Combine(_testFolder, "attributes_multiple_test.wiki");
        var content = "[author: John] [status: draft] [date: 2024-01-15]";
        File.WriteAllText(path, content);

        // Act
        var wiki = new WikidpadWiki(_testFolder);
        var sut = new WikidpadPage(path, wiki);
        var attributes = sut.GetAttributes();

        // Assert
        Assert.Equal(3, attributes.Count);
        Assert.Equal("John", attributes["author"]);
        Assert.Equal("draft", attributes["status"]);
        Assert.Equal("2024-01-15", attributes["date"]);
    }

    [Fact]
    public void GetAttributes_ReturnsEmptyWhenNoAttributes()
    {
        // Arrange
        var path = Path.Combine(_testFolder, "attributes_empty_test.wiki");
        var content = "Just some plain text";
        File.WriteAllText(path, content);

        // Act
        var wiki = new WikidpadWiki(_testFolder);
        var sut = new WikidpadPage(path, wiki);
        var attributes = sut.GetAttributes();

        // Assert
        Assert.Equal(0, attributes.Count);
    }

    [Fact]
    public void GetAttributes_HandlesAttributesWithHyphens()
    {
        // Arrange
        var path = Path.Combine(_testFolder, "attributes_hyphens_test.wiki");
        var content = "[created-date: 2024-01-15] [author-name: John]";
        File.WriteAllText(path, content);

        // Act
        var wiki = new WikidpadWiki(_testFolder);
        var sut = new WikidpadPage(path, wiki);
        var attributes = sut.GetAttributes();

        // Assert
        Assert.Equal(2, attributes.Count);
        Assert.Equal("2024-01-15", attributes["created-date"]);
        Assert.Equal("John", attributes["author-name"]);
    }

    [Fact]
    public void GetAttributes_OverwritesDuplicateKeys()
    {
        // Arrange
        var path = Path.Combine(_testFolder, "attributes_duplicate_test.wiki");
        var content = "[status: draft] [status: final]";
        File.WriteAllText(path, content);

        // Act
        var wiki = new WikidpadWiki(_testFolder);
        var sut = new WikidpadPage(path, wiki);
        var attributes = sut.GetAttributes();

        // Assert
        Assert.Equal(1, attributes.Count);
        Assert.Equal("final", attributes["status"]); // Last value wins
    }
}