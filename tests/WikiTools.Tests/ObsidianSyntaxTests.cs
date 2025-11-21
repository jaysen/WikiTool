using System.Text.RegularExpressions;
using WikiTools;
using Xunit;

namespace WikiTools.Tests;

public class ObsidianSyntaxTests
{
    private readonly ObsidianSyntax _syntax = new ObsidianSyntax();

    #region LinkPattern Tests

    [Fact]
    public void LinkPattern_MatchesBasicWikilink()
    {
        // Arrange
        var input = "[[Page Name]]";

        // Act
        var matches = _syntax.LinkPattern.Matches(input);

        // Assert
        Assert.Equal(1, matches.Count);
        Assert.Equal("Page Name", matches[0].Groups[1].Value);
    }

    [Fact]
    public void LinkPattern_MatchesWikilinkWithDisplayText()
    {
        // Arrange
        var input = "[[Page Name|Display Text]]";

        // Act
        var matches = _syntax.LinkPattern.Matches(input);

        // Assert
        Assert.Equal(1, matches.Count);
        Assert.Equal("Page Name", matches[0].Groups[1].Value);
    }

    [Fact]
    public void LinkPattern_MatchesMultipleLinks()
    {
        // Arrange
        var input = "[[First]] and [[Second|Display]]";

        // Act
        var matches = _syntax.LinkPattern.Matches(input);

        // Assert
        Assert.Equal(2, matches.Count);
        Assert.Equal("First", matches[0].Groups[1].Value);
        Assert.Equal("Second", matches[1].Groups[1].Value);
    }

    #endregion

    #region TagPattern Tests

    [Fact]
    public void TagPattern_MatchesBasicTag()
    {
        // Arrange
        var input = "#important";

        // Act
        var matches = _syntax.TagPattern.Matches(input);

        // Assert
        Assert.Equal(1, matches.Count);
        Assert.Equal("important", matches[0].Groups[1].Value);
    }

    [Fact]
    public void TagPattern_MatchesTagAfterSpace()
    {
        // Arrange
        var input = "text #tag";

        // Act
        var matches = _syntax.TagPattern.Matches(input);

        // Assert
        Assert.Equal(1, matches.Count);
        Assert.Equal("tag", matches[0].Groups[1].Value);
    }

    [Fact]
    public void TagPattern_MatchesTagAtStartOfLine()
    {
        // Arrange
        var input = "#startline";

        // Act
        var matches = _syntax.TagPattern.Matches(input);

        // Assert
        Assert.Equal(1, matches.Count);
        Assert.Equal("startline", matches[0].Groups[1].Value);
    }

    [Fact]
    public void TagPattern_MatchesTagWithHyphens()
    {
        // Arrange
        var input = "#my-tag-name";

        // Act
        var matches = _syntax.TagPattern.Matches(input);

        // Assert
        Assert.Equal(1, matches.Count);
        Assert.Equal("my-tag-name", matches[0].Groups[1].Value);
    }

    [Fact]
    public void TagPattern_MatchesTagWithUnderscores()
    {
        // Arrange
        var input = "#my_tag_name";

        // Act
        var matches = _syntax.TagPattern.Matches(input);

        // Assert
        Assert.Equal(1, matches.Count);
        Assert.Equal("my_tag_name", matches[0].Groups[1].Value);
    }

    [Fact]
    public void TagPattern_DoesNotMatchHashInMiddleOfWord()
    {
        // Arrange
        var input = "word#notag";

        // Act
        var matches = _syntax.TagPattern.Matches(input);

        // Assert
        Assert.Equal(0, matches.Count);
    }

    [Fact]
    public void TagPattern_MatchesMultipleTags()
    {
        // Arrange
        var input = "#first #second #third";

        // Act
        var matches = _syntax.TagPattern.Matches(input);

        // Assert
        Assert.Equal(3, matches.Count);
    }

    #endregion

    #region HeaderPattern Tests

    [Theory]
    [InlineData("# Header", 1, "Header")]
    [InlineData("## Header", 2, "Header")]
    [InlineData("### Header", 3, "Header")]
    [InlineData("#### Deep Header", 4, "Deep Header")]
    [InlineData("###### Very Deep", 6, "Very Deep")]
    public void HeaderPattern_MatchesHeaderLevels(string input, int expectedHashCount, string expectedText)
    {
        // Arrange & Act
        var matches = _syntax.HeaderPattern.Matches(input);

        // Assert
        Assert.Equal(1, matches.Count);
        Assert.Equal(expectedHashCount, matches[0].Groups[1].Value.Length);
        Assert.Equal(expectedText, matches[0].Groups[2].Value);
    }

    [Fact]
    public void HeaderPattern_MatchesMultipleHeaders()
    {
        // Arrange
        var input = "# First\n## Second\n### Third";

        // Act
        var matches = _syntax.HeaderPattern.Matches(input);

        // Assert
        Assert.Equal(3, matches.Count);
    }

    [Fact]
    public void HeaderPattern_RequiresStartOfLine()
    {
        // Arrange
        var input = "text # Header";

        // Act
        var matches = _syntax.HeaderPattern.Matches(input);

        // Assert
        Assert.Equal(0, matches.Count);
    }

    #endregion

    #region YamlPattern Tests

    [Fact]
    public void YamlPattern_MatchesBasicFrontmatter()
    {
        // Arrange
        var input = @"---
title: My Page
---
Content";

        // Act
        var matches = ObsidianSyntax.YamlPattern.Matches(input);

        // Assert
        Assert.Equal(1, matches.Count);
        Assert.Contains("title: My Page", matches[0].Groups[1].Value);
    }

    [Fact]
    public void YamlPattern_MatchesMultilineFrontmatter()
    {
        // Arrange
        var input = @"---
title: My Page
tags: [tag1, tag2]
author: John
---
Content";

        // Act
        var matches = ObsidianSyntax.YamlPattern.Matches(input);

        // Assert
        Assert.Equal(1, matches.Count);
        var frontmatter = matches[0].Groups[1].Value;
        Assert.Contains("title: My Page", frontmatter);
        Assert.Contains("tags: [tag1, tag2]", frontmatter);
        Assert.Contains("author: John", frontmatter);
    }

    [Fact]
    public void YamlPattern_RequiresStartOfFile()
    {
        // Arrange
        var input = @"Some text
---
title: My Page
---";

        // Act
        var matches = ObsidianSyntax.YamlPattern.Matches(input);

        // Assert
        Assert.Equal(0, matches.Count);
    }

    #endregion

    #region YamlTagPattern Tests

    [Fact]
    public void YamlTagPattern_MatchesSingleTag()
    {
        // Arrange
        var frontmatter = "tags: [important]";

        // Act
        var matches = ObsidianSyntax.YamlTagPattern.Matches(frontmatter);

        // Assert
        Assert.Equal(1, matches.Count);
        Assert.Equal("important", matches[0].Groups[1].Value);
    }

    [Fact]
    public void YamlTagPattern_MatchesMultipleTags()
    {
        // Arrange
        var frontmatter = "tags: [first, second, third]";

        // Act
        var matches = ObsidianSyntax.YamlTagPattern.Matches(frontmatter);

        // Assert
        Assert.Equal(1, matches.Count);
        Assert.Equal("first, second, third", matches[0].Groups[1].Value);
    }

    [Fact]
    public void YamlTagPattern_MatchesTagsWithSpaces()
    {
        // Arrange
        var frontmatter = "tags: [tag one, tag two]";

        // Act
        var matches = ObsidianSyntax.YamlTagPattern.Matches(frontmatter);

        // Assert
        Assert.Equal(1, matches.Count);
        Assert.Contains("tag one", matches[0].Groups[1].Value);
    }

    #endregion

    #region YamlAttributePattern Tests

    [Fact]
    public void YamlAttributePattern_MatchesSimpleKeyValue()
    {
        // Arrange
        var frontmatter = "author: John Doe";

        // Act
        var matches = ObsidianSyntax.YamlAttributePattern.Matches(frontmatter);

        // Assert
        Assert.Equal(1, matches.Count);
        Assert.Equal("author", matches[0].Groups[1].Value);
        Assert.Equal("John Doe", matches[0].Groups[2].Value);
    }

    [Fact]
    public void YamlAttributePattern_MatchesMultipleAttributes()
    {
        // Arrange
        var frontmatter = @"title: My Title
author: John
date: 2024-01-15";

        // Act
        var matches = ObsidianSyntax.YamlAttributePattern.Matches(frontmatter);

        // Assert
        Assert.Equal(3, matches.Count);
        Assert.Equal("title", matches[0].Groups[1].Value);
        Assert.Equal("author", matches[1].Groups[1].Value);
        Assert.Equal("date", matches[2].Groups[1].Value);
    }

    [Fact]
    public void YamlAttributePattern_MatchesAttributeWithHyphens()
    {
        // Arrange
        var frontmatter = "created-date: 2024-01-15";

        // Act
        var matches = ObsidianSyntax.YamlAttributePattern.Matches(frontmatter);

        // Assert
        Assert.Equal(1, matches.Count);
        Assert.Equal("created-date", matches[0].Groups[1].Value);
        Assert.Equal("2024-01-15", matches[0].Groups[2].Value);
    }

    [Fact]
    public void YamlAttributePattern_MatchesArrayValue()
    {
        // Arrange
        var frontmatter = "aliases: [alias1, alias2]";

        // Act
        var matches = ObsidianSyntax.YamlAttributePattern.Matches(frontmatter);

        // Assert
        Assert.Equal(1, matches.Count);
        Assert.Equal("aliases", matches[0].Groups[1].Value);
        Assert.Equal("[alias1, alias2]", matches[0].Groups[2].Value);
    }

    #endregion

    #region AliasPattern Tests

    [Fact]
    public void AliasPattern_MatchesSingleAlias()
    {
        // Arrange
        var frontmatter = "aliases: [My Alias]";

        // Act
        var matches = _syntax.AliasPattern.Matches(frontmatter);

        // Assert
        Assert.Equal(1, matches.Count);
        Assert.Equal("My Alias", matches[0].Groups[1].Value);
    }

    [Fact]
    public void AliasPattern_MatchesMultipleAliases()
    {
        // Arrange
        var frontmatter = "aliases: [First, Second, Third]";

        // Act
        var matches = _syntax.AliasPattern.Matches(frontmatter);

        // Assert
        Assert.Equal(1, matches.Count);
        Assert.Equal("First, Second, Third", matches[0].Groups[1].Value);
    }

    #endregion

    #region AttributePattern Tests

    [Fact]
    public void AttributePattern_MatchesBasicInlineAttribute()
    {
        // Arrange
        var input = "[author:: John Doe]";

        // Act
        var matches = _syntax.AttributePattern.Matches(input);

        // Assert
        Assert.Equal(1, matches.Count);
        Assert.Equal("author", matches[0].Groups[1].Value);
        Assert.Equal("John Doe", matches[0].Groups[2].Value);
    }

    [Fact]
    public void AttributePattern_MatchesAttributeWithHyphens()
    {
        // Arrange
        var input = "[created-date:: 2024-01-15]";

        // Act
        var matches = _syntax.AttributePattern.Matches(input);

        // Assert
        Assert.Equal(1, matches.Count);
        Assert.Equal("created-date", matches[0].Groups[1].Value);
        Assert.Equal("2024-01-15", matches[0].Groups[2].Value);
    }

    [Fact]
    public void AttributePattern_MatchesAttributeWithUnderscores()
    {
        // Arrange
        var input = "[created_date:: 2024-01-15]";

        // Act
        var matches = _syntax.AttributePattern.Matches(input);

        // Assert
        Assert.Equal(1, matches.Count);
        Assert.Equal("created_date", matches[0].Groups[1].Value);
    }

    [Fact]
    public void AttributePattern_MatchesAttributeWithNumbers()
    {
        // Arrange
        var input = "[version:: 1.0.0]";

        // Act
        var matches = _syntax.AttributePattern.Matches(input);

        // Assert
        Assert.Equal(1, matches.Count);
        Assert.Equal("version", matches[0].Groups[1].Value);
        Assert.Equal("1.0.0", matches[0].Groups[2].Value);
    }

    [Fact]
    public void AttributePattern_MatchesMultipleAttributes()
    {
        // Arrange
        var input = "[author:: John] [status:: draft]";

        // Act
        var matches = _syntax.AttributePattern.Matches(input);

        // Assert
        Assert.Equal(2, matches.Count);
    }

    [Fact]
    public void AttributePattern_HandlesSpacesInValue()
    {
        // Arrange
        var input = "[description:: This is a long description]";

        // Act
        var matches = _syntax.AttributePattern.Matches(input);

        // Assert
        Assert.Equal(1, matches.Count);
        Assert.Equal("description", matches[0].Groups[1].Value);
        Assert.Equal("This is a long description", matches[0].Groups[2].Value);
    }

    [Fact]
    public void AttributePattern_DoesNotMatchSingleColon()
    {
        // Arrange
        var input = "[author: John]"; // WikidPad syntax, not Obsidian

        // Act
        var matches = _syntax.AttributePattern.Matches(input);

        // Assert
        Assert.Equal(0, matches.Count);
    }

    [Fact]
    public void AttributePattern_MatchesAttributeWithSimpleValue()
    {
        // Arrange
        // Note: The current regex pattern [^\]]+ stops at the first ],
        // so it cannot fully match values containing ] (like links).
        // This is a known limitation that could be improved with a more complex regex.
        var input = "[related:: simple value]";

        // Act
        var matches = _syntax.AttributePattern.Matches(input);

        // Assert
        Assert.Equal(1, matches.Count);
        Assert.Equal("related", matches[0].Groups[1].Value);
        Assert.Equal("simple value", matches[0].Groups[2].Value);
    }

    #endregion
}
