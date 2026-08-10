using WikiTool.Converters;
using Xunit;

namespace WikiTool.Tests;

public class SlugTests
{
    [Theory]
    [InlineData("Alpha", "alpha")]
    [InlineData("My Page", "my-page")]
    [InlineData("C# Notes", "c-sharp-notes")]
    [InlineData("C++ Guide", "c-plus-plus-guide")]
    [InlineData("Already-Slugged", "already-slugged")]
    [InlineData("Lots   Of   Spaces", "lots-of-spaces")]
    [InlineData("Trailing punctuation!!!", "trailing-punctuation")]
    [InlineData("Café Résumé", "cafe-resume")]
    public void ForFile_ProducesUrlSafeSlug(string input, string expected)
    {
        Assert.Equal(expected, Slug.ForFile(input));
    }

    /// <summary>
    /// Jekyll silently excludes files and folders beginning with '_' or '.', which would
    /// drop pages from the published site with no error, so slugs must never start with one.
    /// </summary>
    [Theory]
    [InlineData("_Private Notes")]
    [InlineData(".hidden")]
    [InlineData("__init__")]
    public void ForFile_NeverStartsWithCharacterJekyllExcludes(string input)
    {
        var slug = Slug.ForFile(input);

        Assert.False(slug.StartsWith("_"));
        Assert.False(slug.StartsWith("."));
        Assert.False(slug.StartsWith("-"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("!!!")]
    [InlineData("...")]
    public void ForFile_FallsBackWhenNothingSurvives(string input)
    {
        Assert.Equal("page", Slug.ForFile(input));
    }

    [Fact]
    public void ForPath_SlugsEverySegment()
    {
        Assert.Equal("my projects/sub folder", Slug.ForPath("My Projects/Sub Folder").Replace("-", " "));
        Assert.Equal("my-projects/sub-folder", Slug.ForPath("My Projects/Sub Folder"));
    }

    [Fact]
    public void ForPath_HandlesEmptyPath()
    {
        Assert.Equal("", Slug.ForPath(""));
    }

    [Theory]
    [InlineData("Setup", "setup")]
    [InlineData("Getting Started", "getting-started")]
    [InlineData("What's New?", "whats-new")]
    [InlineData("Step 1: Install", "step-1-install")]
    public void ForAnchor_MatchesKramdownHeadingIds(string input, string expected)
    {
        Assert.Equal(expected, Slug.ForAnchor(input));
    }

    /// <summary>
    /// kramdown prefixes ids that would otherwise start with a digit.
    /// </summary>
    [Fact]
    public void ForAnchor_PrefixesIdsNotStartingWithALetter()
    {
        Assert.Equal("section-2024-review", Slug.ForAnchor("2024 Review"));
    }
}
