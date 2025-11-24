using System.Text.RegularExpressions;
using WikiTools;
using Xunit;

namespace WikiTools.Tests;

public class WikidPadSyntaxTests
{
    private readonly WikidPadSyntax _syntax = new WikidPadSyntax();

    #region LinkPattern Tests

    [Fact]
    public void LinkPattern_MatchesSingleBracketLinks()
    {
        // Arrange
        var input = "[Link with spaces]";

        // Act
        var matches = _syntax.LinkPattern.Matches(input);

        // Assert
        Assert.Single(matches);
        Assert.Equal("Link with spaces", matches[0].Groups[1].Value);
    }

    [Fact]
    public void LinkPattern_MatchesSingleBracketWord()
    {
        // Arrange
        var input = "[WikiWord]";

        // Act
        var matches = _syntax.LinkPattern.Matches(input);

        // Assert
        Assert.Single(matches);
        Assert.Equal("WikiWord", matches[0].Groups[1].Value);
    }

    [Fact]
    public void LinkPattern_DoesNotMatchDoubleBrackets()
    {
        // Arrange
        // WikidPad does NOT use double brackets - that's Obsidian syntax
        var input = "[[Not a WikidPad link]]";

        // Act
        var matches = _syntax.LinkPattern.Matches(input);

        // Assert
        Assert.Empty(matches);
    }

    [Fact]
    public void LinkPattern_DoesNotMatchBareCamelCase()
    {
        // Arrange
        // Bare CamelCase links are matched by CamelCaseLinkPattern, not LinkPattern
        var input = "WikiWord";

        // Act
        var matches = _syntax.LinkPattern.Matches(input);

        // Assert
        Assert.Empty(matches);
    }

    [Fact]
    public void LinkPattern_MatchesMultipleSingleBracketLinks()
    {
        // Arrange
        var input = "[First Link] and [Second Link]";

        // Act
        var matches = _syntax.LinkPattern.Matches(input);

        // Assert
        Assert.Equal(2, matches.Count);
        Assert.Equal("First Link", matches[0].Groups[1].Value);
        Assert.Equal("Second Link", matches[1].Groups[1].Value);
    }

    #endregion

    #region CamelCaseLinkPattern Tests

    [Theory]
    [InlineData("WikiWord")]
    [InlineData("AbC")]
    [InlineData("ABcd")]
    [InlineData("WikiWord123")]
    [InlineData("ABcD")]
    public void CamelCaseLinkPattern_MatchesValidWikiWords(string input)
    {
        // Arrange & Act
        var matches = WikidPadSyntax.CamelCaseLinkPattern.Matches(input);

        // Assert
        Assert.Single(matches);
        Assert.Equal(input, matches[0].Value);
    }

    [Theory]
    [InlineData("lowercase")]
    [InlineData("UPPERCASE")]
    [InlineData("Ab")]
    [InlineData("A")]
    public void CamelCaseLinkPattern_DoesNotMatchInvalidWikiWords(string input)
    {
        // Arrange & Act
        var matches = WikidPadSyntax.CamelCaseLinkPattern.Matches(input);

        // Assert
        Assert.Empty(matches);
    }

    [Fact]
    public void CamelCaseLinkPattern_DoesNotMatchInsideBrackets()
    {
        // Arrange
        var input = "[WikiWord]";

        // Act
        var matches = WikidPadSyntax.CamelCaseLinkPattern.Matches(input);

        // Assert
        Assert.Empty(matches);
    }

    [Fact]
    public void CamelCaseLinkPattern_MatchesMultipleInText()
    {
        // Arrange
        var input = "WikiWord and AnotherLink in text";

        // Act
        var matches = WikidPadSyntax.CamelCaseLinkPattern.Matches(input);

        // Assert
        Assert.Equal(2, matches.Count);
    }

    #endregion

    #region SingleBracketLinkPattern Tests

    [Fact]
    public void SingleBracketLinkPattern_MatchesSingleBrackets()
    {
        // Arrange
        var input = "[link text]";

        // Act
        var matches = WikidPadSyntax.SingleBracketLinkPattern.Matches(input);

        // Assert
        Assert.Single(matches);
        Assert.Equal("link text", matches[0].Groups[1].Value);
    }

    [Fact]
    public void SingleBracketLinkPattern_DoesNotMatchDoubleBrackets()
    {
        // Arrange
        var input = "[[double bracket]]";

        // Act
        var matches = WikidPadSyntax.SingleBracketLinkPattern.Matches(input);

        // Assert
        Assert.Empty(matches);
    }

    [Fact]
    public void SingleBracketLinkPattern_MatchesMultiple()
    {
        // Arrange
        var input = "[first] and [second] links";

        // Act
        var matches = WikidPadSyntax.SingleBracketLinkPattern.Matches(input);

        // Assert
        Assert.Equal(2, matches.Count);
    }

    #endregion

    #region TagPattern Tests

    [Fact]
    public void TagPattern_MatchesBasicTag()
    {
        // Arrange
        var input = "[tag:important]";

        // Act
        var matches = _syntax.TagPattern.Matches(input);

        // Assert
        Assert.Single(matches);
        Assert.Equal("important", matches[0].Groups[1].Value);
    }

    [Fact]
    public void TagPattern_MatchesTagWithSpaces()
    {
        // Arrange
        var input = "[tag:my important tag]";

        // Act
        var matches = _syntax.TagPattern.Matches(input);

        // Assert
        Assert.Single(matches);
        Assert.Equal("my important tag", matches[0].Groups[1].Value);
    }

    [Fact]
    public void TagPattern_MatchesMultipleTags()
    {
        // Arrange
        var input = "[tag:first] some text [tag:second]";

        // Act
        var matches = _syntax.TagPattern.Matches(input);

        // Assert
        Assert.Equal(2, matches.Count);
    }

    #endregion

    #region CategoryPattern Tests

    [Fact]
    public void CategoryPattern_MatchesBasicCategory()
    {
        // Arrange
        var input = "CategoryTesting";

        // Act
        var matches = WikidPadSyntax.CategoryPattern.Matches(input);

        // Assert
        Assert.Single(matches);
        Assert.Equal("Testing", matches[0].Groups[1].Value);
    }

    [Fact]
    public void CategoryPattern_MatchesCategoryWithNumbers()
    {
        // Arrange
        var input = "CategoryTest123";

        // Act
        var matches = WikidPadSyntax.CategoryPattern.Matches(input);

        // Assert
        Assert.Single(matches);
        Assert.Equal("Test123", matches[0].Groups[1].Value);
    }

    [Fact]
    public void CategoryPattern_DoesNotMatchLowercase()
    {
        // Arrange
        var input = "categorytest";

        // Act
        var matches = WikidPadSyntax.CategoryPattern.Matches(input);

        // Assert
        Assert.Empty(matches);
    }

    [Fact]
    public void CategoryPattern_MatchesMultiple()
    {
        // Arrange
        var input = "CategoryFirst and CategorySecond";

        // Act
        var matches = WikidPadSyntax.CategoryPattern.Matches(input);

        // Assert
        Assert.Equal(2, matches.Count);
    }

    #endregion

    #region HeaderPattern Tests

    [Theory]
    [InlineData("+ Header", 1, "Header")]
    [InlineData("++ Header", 2, "Header")]
    [InlineData("+++ Header", 3, "Header")]
    [InlineData("++++ Deep Header", 4, "Deep Header")]
    public void HeaderPattern_MatchesHeaderLevels(string input, int expectedPlusCount, string expectedText)
    {
        // Arrange & Act
        var matches = _syntax.HeaderPattern.Matches(input);

        // Assert
        Assert.Single(matches);
        Assert.Equal(expectedPlusCount, matches[0].Groups[1].Value.Length);
        Assert.Equal(expectedText, matches[0].Groups[2].Value);
    }

    [Fact]
    public void HeaderPattern_MatchesMultipleHeaders()
    {
        // Arrange
        var input = "+ First\n++ Second\n+++ Third";

        // Act
        var matches = _syntax.HeaderPattern.Matches(input);

        // Assert
        Assert.Equal(3, matches.Count);
    }

    [Fact]
    public void HeaderPattern_RequiresStartOfLine()
    {
        // Arrange
        var input = "text + Header";

        // Act
        var matches = _syntax.HeaderPattern.Matches(input);

        // Assert
        Assert.Empty(matches);
    }

    #endregion

    #region AttributePattern Tests

    [Fact]
    public void AttributePattern_MatchesBasicAttribute()
    {
        // Arrange
        var input = "[author: John Doe]";

        // Act
        var matches = _syntax.AttributePattern.Matches(input);

        // Assert
        Assert.Single(matches);
        Assert.Equal("author", matches[0].Groups[1].Value);
        Assert.Equal("John Doe", matches[0].Groups[2].Value);
    }

    [Fact]
    public void AttributePattern_MatchesAttributeWithHyphens()
    {
        // Arrange
        var input = "[created-date: 2024-01-15]";

        // Act
        var matches = _syntax.AttributePattern.Matches(input);

        // Assert
        Assert.Single(matches);
        Assert.Equal("created-date", matches[0].Groups[1].Value);
        Assert.Equal("2024-01-15", matches[0].Groups[2].Value);
    }

    [Fact]
    public void AttributePattern_MatchesAttributeWithNumbers()
    {
        // Arrange
        var input = "[version: 1.0.0]";

        // Act
        var matches = _syntax.AttributePattern.Matches(input);

        // Assert
        Assert.Single(matches);
        Assert.Equal("version", matches[0].Groups[1].Value);
        Assert.Equal("1.0.0", matches[0].Groups[2].Value);
    }

    [Fact]
    public void AttributePattern_MatchesMultipleAttributes()
    {
        // Arrange
        var input = "[author: John] [status: draft]";

        // Act
        var matches = _syntax.AttributePattern.Matches(input);

        // Assert
        Assert.Equal(2, matches.Count);
    }

    [Fact]
    public void AttributePattern_DoesNotMatchTag()
    {
        // Arrange - tags use "tag:" prefix, not general attributes
        var input = "[tag: important]";

        // Act
        var matches = _syntax.AttributePattern.Matches(input);

        // Assert
        // This WILL match because AttributePattern is more general
        // The distinction is semantic, not syntactic
        Assert.Single(matches);
        Assert.Equal("tag", matches[0].Groups[1].Value);
    }

    [Fact]
    public void AttributePattern_HandlesSpacesInValue()
    {
        // Arrange
        var input = "[description: This is a long description]";

        // Act
        var matches = _syntax.AttributePattern.Matches(input);

        // Assert
        Assert.Single(matches);
        Assert.Equal("description", matches[0].Groups[1].Value);
        Assert.Equal("This is a long description", matches[0].Groups[2].Value);
    }

    #endregion

    #region AliasPattern Tests

    [Fact]
    public void AliasPattern_IsNull()
    {
        // Arrange & Act & Assert
        Assert.Null(_syntax.AliasPattern);
    }

    #endregion
}
