using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using WikiTool.Pages;
using WikiTool.Wikis;

namespace WikiTool.Converters;

/// <summary>
/// Converts Obsidian wiki format to standard Markdown wiki format (GitHub Pages compatible).
///
/// Key conversions:
/// - [[link]] -> [link](link.md) with intelligent path resolution
/// - [[link|display]] -> [display](link.md)
/// - [[link#heading]] -> [link](link.md#heading)
/// - Resolves links using wiki page index for accurate relative paths
/// - Handles ambiguous links via callback
/// </summary>
public partial class ObsidianToMarkdownWikiConverter
{
    public string SourcePath { get; }
    public string DestinationPath { get; }

    private readonly ObsidianWiki _sourceWiki;
    private readonly ObsidianSyntax _sourceSyntax;

    /// <summary>
    /// Callback for resolving ambiguous links when multiple pages have the same name.
    /// Parameters: linkText, possiblePaths, sourceFilePath
    /// Returns: chosen path from possiblePaths
    /// </summary>
    public Func<string, List<string>, string, string> OnAmbiguousLink { get; set; }

    /// <summary>
    /// Pattern for matching Obsidian wikilinks: [[link]] or [[link|display]] or [[link#heading]]
    /// Captures: Group 1 = full link target (may include #heading), Group 2 = display text (optional)
    /// </summary>
    [GeneratedRegex(@"\[\[([^\]|#\r\n]+)(#[^\]|\r\n]+)?(?:\|([^\]\r\n]*))?\]\]")]
    private static partial Regex ObsidianLinkPattern();

    public ObsidianToMarkdownWikiConverter(string sourcePath, string destinationPath)
    {
        if (!Directory.Exists(sourcePath))
        {
            throw new DirectoryNotFoundException($"Source directory not found: {sourcePath}");
        }

        SourcePath = sourcePath;
        DestinationPath = destinationPath;

        _sourceWiki = new ObsidianWiki(sourcePath);
        _sourceSyntax = _sourceWiki.Syntax as ObsidianSyntax;
    }

    /// <summary>
    /// Convert all Obsidian wiki files to Markdown wiki format
    /// </summary>
    public void ConvertAll()
    {
        if (!Directory.Exists(DestinationPath))
        {
            Directory.CreateDirectory(DestinationPath);
        }

        var pages = _sourceWiki.GetAllPages();

        foreach (var page in pages)
        {
            ConvertPage(page);
        }
    }

    /// <summary>
    /// Convert a single Obsidian page to Markdown wiki format
    /// </summary>
    private void ConvertPage(Page page)
    {
        var content = page.GetContent();

        // Get source file path for relative path calculation
        var sourceFilePath = page is LocalPage localPage
            ? Path.GetRelativePath(SourcePath, localPage.PagePath)
            : page.Name + ".md";

        var convertedContent = ConvertContent(content, sourceFilePath);

        // Preserve folder structure in destination
        string outputPath;
        if (page is LocalPage local)
        {
            var relativePath = Path.GetRelativePath(SourcePath, local.PagePath);
            var directory = Path.GetDirectoryName(relativePath);
            var fileName = MarkdownWikiSyntax.ToFilePath(page.Name) + ".md";

            if (!string.IsNullOrEmpty(directory))
            {
                var destDirectory = Path.Combine(DestinationPath, directory);
                Directory.CreateDirectory(destDirectory);
                outputPath = Path.Combine(destDirectory, fileName);
            }
            else
            {
                outputPath = Path.Combine(DestinationPath, fileName);
            }
        }
        else
        {
            // Fallback for non-local pages
            var fileName = MarkdownWikiSyntax.ToFilePath(page.Name) + ".md";
            outputPath = Path.Combine(DestinationPath, fileName);
        }

        File.WriteAllText(outputPath, convertedContent);
    }

    /// <summary>
    /// Convert Obsidian content to standard Markdown wiki format
    /// </summary>
    public string ConvertContent(string content, string sourceFilePath = null)
    {
        if (string.IsNullOrEmpty(content))
        {
            return content;
        }

        // Convert Obsidian wikilinks to standard markdown links
        content = ConvertLinks(content, sourceFilePath);

        return content;
    }

    /// <summary>
    /// Convert Obsidian wikilinks to standard markdown links with intelligent path resolution
    /// [[link]] -> [link](relative/path/to/link.md)
    /// [[link|display]] -> [display](relative/path/to/link.md)
    /// [[link#heading]] -> [link](relative/path/to/link.md#heading)
    /// </summary>
    public string ConvertLinks(string content, string sourceFilePath = null)
    {
        return ObsidianLinkPattern().Replace(content, match =>
        {
            var linkTarget = match.Groups[1].Value.Trim();
            var heading = match.Groups[2].Success ? match.Groups[2].Value : string.Empty;
            var displayText = match.Groups[3].Success ? match.Groups[3].Value.Trim() : null;

            // Resolve the link to an actual file path using the page index
            var filePath = ResolveLinkToPath(linkTarget, sourceFilePath);

            // Convert heading anchor (spaces to hyphens, lowercase)
            if (!string.IsNullOrEmpty(heading))
            {
                heading = heading.ToLowerInvariant().Replace(" ", "-");
                filePath += heading;
            }

            // Use display text if provided, otherwise use original link target
            var text = !string.IsNullOrEmpty(displayText) ? displayText : linkTarget;

            return $"[{text}]({filePath})";
        });
    }

    /// <summary>
    /// Resolves a wiki link to its actual file path using the page index.
    /// Returns a relative path from sourceFile to the target file.
    /// </summary>
    private string ResolveLinkToPath(string linkText, string sourceFilePath)
    {
        var index = _sourceWiki.PageNameIndex;

        // Try to find the page in the index
        if (index.TryGetValue(linkText, out var matchingPaths))
        {
            if (matchingPaths.Count == 1)
            {
                // Single match - calculate relative path if source is known
                var targetPath = matchingPaths[0];

                if (!string.IsNullOrEmpty(sourceFilePath))
                {
                    return CalculateRelativePath(sourceFilePath, targetPath);
                }

                // No source path - return simple conversion
                return ConvertToMarkdownPath(targetPath);
            }
            else if (matchingPaths.Count > 1)
            {
                // Multiple matches - use callback if available
                if (OnAmbiguousLink != null)
                {
                    var chosenPath = OnAmbiguousLink(linkText, matchingPaths, sourceFilePath ?? "");

                    if (!string.IsNullOrEmpty(sourceFilePath))
                    {
                        return CalculateRelativePath(sourceFilePath, chosenPath);
                    }

                    return ConvertToMarkdownPath(chosenPath);
                }

                // No callback - just use first match
                var targetPath = matchingPaths[0];

                if (!string.IsNullOrEmpty(sourceFilePath))
                {
                    return CalculateRelativePath(sourceFilePath, targetPath);
                }

                return ConvertToMarkdownPath(targetPath);
            }
        }

        // No match found - return simple conversion (page might not exist yet)
        return MarkdownWikiSyntax.ToFilePath(linkText) + ".md";
    }

    /// <summary>
    /// Calculates the relative path from source file to target file.
    /// Both paths should be relative to the wiki root.
    /// </summary>
    private static string CalculateRelativePath(string fromPath, string toPath)
    {
        // Get directory of source file
        var sourceDir = Path.GetDirectoryName(fromPath) ?? "";

        // Convert target path to markdown format
        var targetMdPath = ConvertToMarkdownPath(toPath);

        // If source is at root, just return the target
        if (string.IsNullOrEmpty(sourceDir))
        {
            return targetMdPath;
        }

        // Calculate relative path
        var relativePath = Path.GetRelativePath(sourceDir, targetMdPath);

        // Normalize path separators to forward slashes for markdown
        return relativePath.Replace(Path.DirectorySeparatorChar, '/');
    }

    /// <summary>
    /// Converts a source path to markdown-compatible format (lowercase, hyphens)
    /// </summary>
    private static string ConvertToMarkdownPath(string path)
    {
        var directory = Path.GetDirectoryName(path);
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(path);
        var convertedName = MarkdownWikiSyntax.ToFilePath(fileNameWithoutExt) + ".md";

        if (!string.IsNullOrEmpty(directory))
        {
            return Path.Combine(directory, convertedName);
        }

        return convertedName;
    }

    /// <summary>
    /// Convert a single link target to a valid file path
    /// </summary>
    public static string ConvertLinkTarget(string linkTarget)
    {
        return MarkdownWikiSyntax.ToFilePath(linkTarget);
    }
}
