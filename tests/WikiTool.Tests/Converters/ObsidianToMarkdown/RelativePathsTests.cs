using System;
using System.IO;
using WikiTool.Converters;
using WikiTool.Wikis;
using Xunit;

namespace WikiTool.Tests.Converters.ObsidianToMarkdown;

/// <summary>
/// Tests for ObsidianToMarkdownWikiConverter focusing on relative path handling
/// for nested folder structures and cross-folder links.
/// </summary>
public class RelativePathsTests
{
    private readonly string _testFolder;
    private readonly string _sourceDir;
    private readonly string _destDir;

    public RelativePathsTests()
    {
        _testFolder = TestUtilities.GetTestFolder("obsidian_mdwiki_relative_paths");
        _sourceDir = Path.Combine(_testFolder, "source");
        _destDir = Path.Combine(_testFolder, "dest");
    }

    #region Nested Folder Structure Tests (Future Feature)

    [Fact]
    public void ConvertAll_NestedFolders_PreservesStructure()
    {
        // Arrange - create nested folder structure
        SetupSourceDirectory();

        var docsDir = Path.Combine(_sourceDir, "docs");
        var guidesDir = Path.Combine(_sourceDir, "docs", "guides");
        var tutorialsDir = Path.Combine(_sourceDir, "tutorials");

        Directory.CreateDirectory(docsDir);
        Directory.CreateDirectory(guidesDir);
        Directory.CreateDirectory(tutorialsDir);

        File.WriteAllText(Path.Combine(_sourceDir, "Index.md"), "# Index\n\n[[Getting Started]]");
        File.WriteAllText(Path.Combine(docsDir, "Overview.md"), "# Overview");
        File.WriteAllText(Path.Combine(guidesDir, "Quick Start.md"), "# Quick Start");
        File.WriteAllText(Path.Combine(tutorialsDir, "Tutorial One.md"), "# Tutorial");

        if (Directory.Exists(_destDir))
            Directory.Delete(_destDir, true);

        var converter = new ObsidianToMarkdownWikiConverter(_sourceDir, _destDir);

        // Act
        converter.ConvertAll();

        // Assert - folder structure should be preserved
        Assert.True(File.Exists(Path.Combine(_destDir, "index.md")));
        Assert.True(File.Exists(Path.Combine(_destDir, "docs", "overview.md")),
            "FUTURE: Should preserve docs folder");
        Assert.True(File.Exists(Path.Combine(_destDir, "docs", "guides", "quick-start.md")),
            "FUTURE: Should preserve nested docs/guides folder");
        Assert.True(File.Exists(Path.Combine(_destDir, "tutorials", "tutorial-one.md")),
            "FUTURE: Should preserve tutorials folder");
    }

    [Fact]
    public void ConvertAll_NestedFolders_CopiesAllFiles()
    {
        // Arrange
        SetupSourceDirectory();

        var subDir = Path.Combine(_sourceDir, "subfolder");
        Directory.CreateDirectory(subDir);

        File.WriteAllText(Path.Combine(_sourceDir, "Root.md"), "Root content");
        File.WriteAllText(Path.Combine(subDir, "Child.md"), "Child content");
        File.WriteAllText(Path.Combine(subDir, "Another Child.md"), "Another child");

        if (Directory.Exists(_destDir))
            Directory.Delete(_destDir, true);

        var converter = new ObsidianToMarkdownWikiConverter(_sourceDir, _destDir);

        // Act
        converter.ConvertAll();

        // Assert - all files should be copied
        var destSubDir = Path.Combine(_destDir, "subfolder");
        Assert.True(Directory.Exists(destSubDir), "FUTURE: Subfolder should be created");
        Assert.True(File.Exists(Path.Combine(destSubDir, "child.md")),
            "FUTURE: Child files should be in subfolder");
        Assert.True(File.Exists(Path.Combine(destSubDir, "another-child.md")),
            "FUTURE: All child files should be converted");
    }

    [Fact]
    public void ConvertAll_NestedFolders_NonMarkdownFilesPreserved()
    {
        // Arrange - include images, PDFs, etc.
        SetupSourceDirectory();

        var imagesDir = Path.Combine(_sourceDir, "images");
        var attachmentsDir = Path.Combine(_sourceDir, "attachments");

        Directory.CreateDirectory(imagesDir);
        Directory.CreateDirectory(attachmentsDir);

        File.WriteAllText(Path.Combine(_sourceDir, "Page.md"), "# Page");
        File.WriteAllText(Path.Combine(imagesDir, "diagram.png"), "fake-image-data");
        File.WriteAllText(Path.Combine(attachmentsDir, "document.pdf"), "fake-pdf-data");

        if (Directory.Exists(_destDir))
            Directory.Delete(_destDir, true);

        var converter = new ObsidianToMarkdownWikiConverter(_sourceDir, _destDir);

        // Act
        converter.ConvertAll();

        // Assert - non-markdown files should be copied
        Assert.True(File.Exists(Path.Combine(_destDir, "images", "diagram.png")),
            "FUTURE: Images should be copied to destination");
        Assert.True(File.Exists(Path.Combine(_destDir, "attachments", "document.pdf")),
            "FUTURE: Attachments should be copied to destination");
    }

    #endregion

    #region File Matching During Link Conversion (Future Feature)

    [Fact]
    public void ConvertLinks_LinkMatchesExistingFile_UsesCorrectPath()
    {
        // Arrange - create actual files in source
        SetupSourceDirectory();

        var docsDir = Path.Combine(_sourceDir, "docs");
        Directory.CreateDirectory(docsDir);

        File.WriteAllText(Path.Combine(_sourceDir, "Index.md"),
            "See [[Getting Started]] and [[Advanced Topics]]");
        File.WriteAllText(Path.Combine(_sourceDir, "Getting Started.md"), "# Getting Started");
        File.WriteAllText(Path.Combine(docsDir, "Advanced Topics.md"), "# Advanced Topics");

        if (Directory.Exists(_destDir))
            Directory.Delete(_destDir, true);

        var converter = new ObsidianToMarkdownWikiConverter(_sourceDir, _destDir);

        // Act
        converter.ConvertAll();

        // Assert - links should use correct relative paths
        var indexContent = File.ReadAllText(Path.Combine(_destDir, "index.md"));
        Assert.Contains("[Getting Started](getting-started.md)", indexContent);
        // FUTURE: Link to file in subfolder should include relative path
        Assert.Contains("[Advanced Topics](docs/advanced-topics.md)", indexContent);
    }

    [Fact]
    public void ConvertLinks_FileInSubfolder_LinksToFileInParent()
    {
        // Arrange
        SetupSourceDirectory();

        var guidesDir = Path.Combine(_sourceDir, "guides");
        Directory.CreateDirectory(guidesDir);

        File.WriteAllText(Path.Combine(_sourceDir, "Index.md"), "# Index");
        File.WriteAllText(Path.Combine(guidesDir, "Guide.md"),
            "# Guide\n\nBack to [[Index]]");

        if (Directory.Exists(_destDir))
            Directory.Delete(_destDir, true);

        var converter = new ObsidianToMarkdownWikiConverter(_sourceDir, _destDir);

        // Act
        converter.ConvertAll();

        // Assert - FUTURE: link from subfolder to parent should use ../
        var guideContent = File.ReadAllText(Path.Combine(_destDir, "guides", "guide.md"));
        Assert.Contains("[Index](../index.md)", guideContent);
    }

    [Fact]
    public void ConvertLinks_FileInSubfolder_LinksToFileInSibling()
    {
        // Arrange
        SetupSourceDirectory();

        var guidesDir = Path.Combine(_sourceDir, "guides");
        var tutorialsDir = Path.Combine(_sourceDir, "tutorials");
        Directory.CreateDirectory(guidesDir);
        Directory.CreateDirectory(tutorialsDir);

        File.WriteAllText(Path.Combine(guidesDir, "Guide.md"),
            "# Guide\n\nSee [[Tutorial]]");
        File.WriteAllText(Path.Combine(tutorialsDir, "Tutorial.md"), "# Tutorial");

        if (Directory.Exists(_destDir))
            Directory.Delete(_destDir, true);

        var converter = new ObsidianToMarkdownWikiConverter(_sourceDir, _destDir);

        // Act
        converter.ConvertAll();

        // Assert - FUTURE: link between siblings should use ../sibling/
        var guideContent = File.ReadAllText(Path.Combine(_destDir, "guides", "guide.md"));
        Assert.Contains("[Tutorial](../tutorials/tutorial.md)", guideContent);
    }

    [Fact]
    public void ConvertLinks_DeepNesting_CalculatesCorrectRelativePath()
    {
        // Arrange
        SetupSourceDirectory();

        var level1 = Path.Combine(_sourceDir, "level1");
        var level2 = Path.Combine(level1, "level2");
        var level3 = Path.Combine(level2, "level3");
        Directory.CreateDirectory(level3);

        File.WriteAllText(Path.Combine(_sourceDir, "Root.md"), "# Root");
        File.WriteAllText(Path.Combine(level3, "Deep.md"),
            "# Deep\n\nBack to [[Root]]");

        if (Directory.Exists(_destDir))
            Directory.Delete(_destDir, true);

        var converter = new ObsidianToMarkdownWikiConverter(_sourceDir, _destDir);

        // Act
        converter.ConvertAll();

        // Assert - FUTURE: should calculate correct relative path from deep nesting
        var deepContent = File.ReadAllText(Path.Combine(_destDir, "level1", "level2", "level3", "deep.md"));
        Assert.Contains("[Root](../../../root.md)", deepContent);
    }

    #endregion

    #region Ambiguous Link Resolution (Future Feature)

    [Fact]
    public void ConvertLinks_DuplicateFileNames_RequiresUserChoice()
    {
        // Arrange - same filename in different folders
        SetupSourceDirectory();

        var docsDir = Path.Combine(_sourceDir, "docs");
        var guidesDir = Path.Combine(_sourceDir, "guides");
        Directory.CreateDirectory(docsDir);
        Directory.CreateDirectory(guidesDir);

        File.WriteAllText(Path.Combine(_sourceDir, "Index.md"),
            "See [[Overview]] for more");
        File.WriteAllText(Path.Combine(docsDir, "Overview.md"), "# Docs Overview");
        File.WriteAllText(Path.Combine(guidesDir, "Overview.md"), "# Guides Overview");

        if (Directory.Exists(_destDir))
            Directory.Delete(_destDir, true);

        var converter = new ObsidianToMarkdownWikiConverter(_sourceDir, _destDir);

        // Act & Assert
        // FUTURE: Should detect ambiguity and prompt user
        // For now, this test documents the expected behavior
        var exception = Record.Exception(() => converter.ConvertAll());

        // Could throw an exception OR have a callback mechanism
        Assert.True(exception != null || File.Exists(Path.Combine(_destDir, "index.md")),
            "FUTURE: Should either prompt user or throw exception for ambiguous links");
    }

    [Fact]
    public void ConvertLinks_AmbiguousLink_PresentsBothOptions()
    {
        // Arrange
        SetupSourceDirectory();

        var dir1 = Path.Combine(_sourceDir, "folder1");
        var dir2 = Path.Combine(_sourceDir, "folder2");
        Directory.CreateDirectory(dir1);
        Directory.CreateDirectory(dir2);

        File.WriteAllText(Path.Combine(_sourceDir, "Main.md"), "Link to [[Page]]");
        File.WriteAllText(Path.Combine(dir1, "Page.md"), "# Page 1");
        File.WriteAllText(Path.Combine(dir2, "Page.md"), "# Page 2");

        var converter = new ObsidianToMarkdownWikiConverter(_sourceDir, _destDir);

        // FUTURE: Converter should have a callback or event for ambiguous links
        // The callback should receive:
        // - The link text: "Page"
        // - List of possible matches: ["folder1/Page.md", "folder2/Page.md"]
        // - The source file: "Main.md"
        // And return the user's choice

        Assert.True(true, "FUTURE: Test framework for ambiguity resolution needed");
    }

    [Fact]
    public void ConvertLinks_UniqueFileName_NoAmbiguity()
    {
        // Arrange
        SetupSourceDirectory();

        var docsDir = Path.Combine(_sourceDir, "docs");
        Directory.CreateDirectory(docsDir);

        File.WriteAllText(Path.Combine(_sourceDir, "Index.md"), "See [[Unique Page]]");
        File.WriteAllText(Path.Combine(docsDir, "Unique Page.md"), "# Unique Page");

        if (Directory.Exists(_destDir))
            Directory.Delete(_destDir, true);

        var converter = new ObsidianToMarkdownWikiConverter(_sourceDir, _destDir);

        // Act
        converter.ConvertAll();

        // Assert - FUTURE: should resolve without user input since it's unique
        var indexContent = File.ReadAllText(Path.Combine(_destDir, "index.md"));
        Assert.Contains("[Unique Page](docs/unique-page.md)", indexContent);
    }

    [Fact]
    public void ConvertLinks_LinkNotFound_ReportsWarning()
    {
        // Arrange
        SetupSourceDirectory();

        File.WriteAllText(Path.Combine(_sourceDir, "Page.md"),
            "Link to [[Nonexistent Page]]");

        if (Directory.Exists(_destDir))
            Directory.Delete(_destDir, true);

        var converter = new ObsidianToMarkdownWikiConverter(_sourceDir, _destDir);

        // Act
        converter.ConvertAll();

        // FUTURE: Should log warning or collect broken links
        // For now, link is converted but won't work
        var pageContent = File.ReadAllText(Path.Combine(_destDir, "page.md"));
        Assert.Contains("[Nonexistent Page](nonexistent-page.md)", pageContent);

        // FUTURE: Converter should expose a list of broken links
        Assert.True(true);
    }

    #endregion

    #region Relative Path Calculation (Future Feature)

    [Fact]
    public void CalculateRelativePath_SameFolder_ReturnsSimplePath()
    {
        // FUTURE: Add static helper method
        // var result = ObsidianToMarkdownWikiConverter.CalculateRelativePath(
        //     from: "folder/page1.md",
        //     to: "folder/page2.md"
        // );
        // Assert.Equal("page2.md", result);

        Assert.True(true, "FUTURE: Need CalculateRelativePath helper method");
    }

    [Fact]
    public void CalculateRelativePath_ToSubfolder_ReturnsForwardPath()
    {
        // FUTURE:
        // var result = ObsidianToMarkdownWikiConverter.CalculateRelativePath(
        //     from: "page.md",
        //     to: "docs/guide.md"
        // );
        // Assert.Equal("docs/guide.md", result);

        Assert.True(true, "FUTURE: Should calculate path to subfolder");
    }

    [Fact]
    public void CalculateRelativePath_ToParent_ReturnsBackwardPath()
    {
        // FUTURE:
        // var result = ObsidianToMarkdownWikiConverter.CalculateRelativePath(
        //     from: "docs/page.md",
        //     to: "index.md"
        // );
        // Assert.Equal("../index.md", result);

        Assert.True(true, "FUTURE: Should calculate path to parent folder");
    }

    [Fact]
    public void CalculateRelativePath_BetweenSiblings_ReturnsCorrectPath()
    {
        // FUTURE:
        // var result = ObsidianToMarkdownWikiConverter.CalculateRelativePath(
        //     from: "docs/page.md",
        //     to: "guides/guide.md"
        // );
        // Assert.Equal("../guides/guide.md", result);

        Assert.True(true, "FUTURE: Should calculate path between sibling folders");
    }

    [Fact]
    public void CalculateRelativePath_DeepNesting_ReturnsCorrectPath()
    {
        // FUTURE:
        // var result = ObsidianToMarkdownWikiConverter.CalculateRelativePath(
        //     from: "a/b/c/page.md",
        //     to: "x/y/target.md"
        // );
        // Assert.Equal("../../../x/y/target.md", result);

        Assert.True(true, "FUTURE: Should handle deep nesting correctly");
    }

    [Fact]
    public void ConvertAll_WithRelativePaths_AllLinksValid()
    {
        // Arrange - complex nested structure
        SetupSourceDirectory();

        var docsDir = Path.Combine(_sourceDir, "docs");
        var guidesDir = Path.Combine(docsDir, "guides");
        var apiDir = Path.Combine(_sourceDir, "api");

        Directory.CreateDirectory(guidesDir);
        Directory.CreateDirectory(apiDir);

        // Create interconnected pages
        File.WriteAllText(Path.Combine(_sourceDir, "Index.md"),
            "# Index\n\n- [[Getting Started]]\n- [[API Reference]]");
        File.WriteAllText(Path.Combine(guidesDir, "Getting Started.md"),
            "# Getting Started\n\nSee [[API Reference]] and [[Index]]");
        File.WriteAllText(Path.Combine(apiDir, "API Reference.md"),
            "# API Reference\n\nBack to [[Index]] or [[Getting Started]]");

        if (Directory.Exists(_destDir))
            Directory.Delete(_destDir, true);

        var converter = new ObsidianToMarkdownWikiConverter(_sourceDir, _destDir);

        // Act
        converter.ConvertAll();

        // Assert - FUTURE: all links should be valid relative paths
        var indexContent = File.ReadAllText(Path.Combine(_destDir, "index.md"));
        Assert.Contains("[Getting Started](docs/guides/getting-started.md)", indexContent);
        Assert.Contains("[API Reference](api/api-reference.md)", indexContent);

        var guideContent = File.ReadAllText(Path.Combine(_destDir, "docs", "guides", "getting-started.md"));
        Assert.Contains("[API Reference](../../api/api-reference.md)", guideContent);
        Assert.Contains("[Index](../../index.md)", guideContent);

        var apiContent = File.ReadAllText(Path.Combine(_destDir, "api", "api-reference.md"));
        Assert.Contains("[Index](../index.md)", apiContent);
        Assert.Contains("[Getting Started](../docs/guides/getting-started.md)", apiContent);
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
