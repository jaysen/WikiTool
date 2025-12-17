using System.IO;
using WikiTool.Wikis;
using Xunit;

namespace WikiTool.Tests.Wikis;

/// <summary>
/// Tests for Wiki.PageNameIndex functionality
/// </summary>
public class WikiPageIndexTests
{
    private readonly string _testFolder;
    private readonly string _wikiPath;

    public WikiPageIndexTests()
    {
        _testFolder = TestUtilities.GetTestFolder("wiki_index_tests");
        _wikiPath = Path.Combine(_testFolder, "test_wiki");
    }

    [Fact]
    public void PageNameIndex_LazyLoaded_BuildsOnFirstAccess()
    {
        // Arrange
        SetupTestWiki();
        File.WriteAllText(Path.Combine(_wikiPath, "TestPage.md"), "# Test Page");
        var wiki = new ObsidianWiki(_wikiPath);

        // Act
        var index = wiki.PageNameIndex;

        // Assert
        Assert.NotNull(index);
        Assert.True(index.Count > 0);
    }

    [Fact]
    public void PageNameIndex_UniquePageNames_SingleEntryPerName()
    {
        // Arrange
        SetupTestWiki();
        File.WriteAllText(Path.Combine(_wikiPath, "PageOne.md"), "# Page One");
        File.WriteAllText(Path.Combine(_wikiPath, "PageTwo.md"), "# Page Two");
        File.WriteAllText(Path.Combine(_wikiPath, "PageThree.md"), "# Page Three");

        var wiki = new ObsidianWiki(_wikiPath);

        // Act
        var index = wiki.PageNameIndex;

        // Assert
        Assert.Equal(3, index.Count);
        Assert.Single(index["PageOne"]);
        Assert.Single(index["PageTwo"]);
        Assert.Single(index["PageThree"]);
        // Verify paths are relative
        Assert.Equal("PageOne.md", index["PageOne"][0]);
        Assert.Equal("PageTwo.md", index["PageTwo"][0]);
    }

    [Fact]
    public void PageNameIndex_DuplicatePageNames_MultipleEntriesPerName()
    {
        // Arrange
        SetupTestWiki();
        var subfolder = Path.Combine(_wikiPath, "docs");
        Directory.CreateDirectory(subfolder);

        File.WriteAllText(Path.Combine(_wikiPath, "Overview.md"), "# Root Overview");
        File.WriteAllText(Path.Combine(subfolder, "Overview.md"), "# Docs Overview");

        var wiki = new ObsidianWiki(_wikiPath);

        // Act
        var index = wiki.PageNameIndex;

        // Assert
        Assert.Single(index); // One entry for "Overview"
        Assert.Equal(2, index["Overview"].Count); // But two paths with that name
        Assert.Contains("Overview.md", index["Overview"]);
        Assert.Contains(Path.Combine("docs", "Overview.md"), index["Overview"]);
    }

    [Fact]
    public void PageNameIndex_CaseInsensitive_MatchesAnyCase()
    {
        // Arrange
        SetupTestWiki();
        File.WriteAllText(Path.Combine(_wikiPath, "MyPage.md"), "# MyPage");

        var wiki = new ObsidianWiki(_wikiPath);

        // Act
        var index = wiki.PageNameIndex;

        // Assert
        Assert.True(index.ContainsKey("MyPage"));
        Assert.True(index.ContainsKey("mypage"));
        Assert.True(index.ContainsKey("MYPAGE"));
        Assert.True(index.ContainsKey("MyPaGe"));
    }

    [Fact]
    public void PageNameIndex_NestedFolders_IncludesAllPages()
    {
        // Arrange
        SetupTestWiki();
        var level1 = Path.Combine(_wikiPath, "level1");
        var level2 = Path.Combine(level1, "level2");
        Directory.CreateDirectory(level2);

        File.WriteAllText(Path.Combine(_wikiPath, "Root.md"), "# Root");
        File.WriteAllText(Path.Combine(level1, "Middle.md"), "# Middle");
        File.WriteAllText(Path.Combine(level2, "Deep.md"), "# Deep");

        var wiki = new ObsidianWiki(_wikiPath);

        // Act
        var index = wiki.PageNameIndex;

        // Assert
        Assert.Equal(3, index.Count);
        Assert.Contains("Root", index.Keys);
        Assert.Contains("Middle", index.Keys);
        Assert.Contains("Deep", index.Keys);
    }

    [Fact]
    public void InvalidatePageIndex_ClearsCache_RebuildsOnNextAccess()
    {
        // Arrange
        SetupTestWiki();
        File.WriteAllText(Path.Combine(_wikiPath, "Initial.md"), "# Initial");

        var wiki = new ObsidianWiki(_wikiPath);
        var firstIndex = wiki.PageNameIndex;
        Assert.Single(firstIndex);

        // Act - add a new file and invalidate
        File.WriteAllText(Path.Combine(_wikiPath, "NewPage.md"), "# New Page");
        wiki.InvalidatePageIndex();
        var secondIndex = wiki.PageNameIndex;

        // Assert
        Assert.Equal(2, secondIndex.Count);
        Assert.Contains("Initial", secondIndex.Keys);
        Assert.Contains("NewPage", secondIndex.Keys);
    }

    [Fact]
    public void PageNameIndex_EmptyWiki_ReturnsEmptyIndex()
    {
        // Arrange
        SetupTestWiki(); // Empty wiki

        var wiki = new ObsidianWiki(_wikiPath);

        // Act
        var index = wiki.PageNameIndex;

        // Assert
        Assert.NotNull(index);
        Assert.Empty(index);
    }

    private void SetupTestWiki()
    {
        if (Directory.Exists(_wikiPath))
        {
            Directory.Delete(_wikiPath, true);
        }
        Directory.CreateDirectory(_wikiPath);
    }
}
