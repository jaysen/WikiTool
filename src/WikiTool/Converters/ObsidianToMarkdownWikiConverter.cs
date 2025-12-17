using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using WikiTool.Pages;
using WikiTool.Wikis;

namespace WikiTool.Converters;

/// <summary>
/// Converts Obsidian wiki format to standard Markdown wiki format (GitHub Pages compatible).
///
/// Key conversions:
/// - [[link]] -> [link](link.md)
/// - [[link|display]] -> [display](link.md)
/// - [[link#heading]] -> [link](link.md#heading)
/// - Spaces in paths are converted to hyphens
/// - All paths are lowercased for URL compatibility
/// </summary>
public partial class ObsidianToMarkdownWikiConverter
{
    public string SourcePath { get; }
    public string DestinationPath { get; }

    private readonly ObsidianWiki _sourceWiki;
    private readonly ObsidianSyntax _sourceSyntax;

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
        var convertedContent = ConvertContent(content);

        // Convert filename: spaces to hyphens, lowercase
        var fileName = MarkdownWikiSyntax.ToFilePath(page.Name) + ".md";
        var outputPath = Path.Combine(DestinationPath, fileName);

        File.WriteAllText(outputPath, convertedContent);
    }

    /// <summary>
    /// Convert Obsidian content to standard Markdown wiki format
    /// </summary>
    public string ConvertContent(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return content;
        }

        // Convert Obsidian wikilinks to standard markdown links
        content = ConvertLinks(content);

        return content;
    }

    /// <summary>
    /// Convert Obsidian wikilinks to standard markdown links
    /// [[link]] -> [link](link.md)
    /// [[link|display]] -> [display](link.md)
    /// [[link#heading]] -> [link](link.md#heading)
    /// [[link#heading|display]] -> [display](link.md#heading)
    /// </summary>
    public string ConvertLinks(string content)
    {
        return ObsidianLinkPattern().Replace(content, match =>
        {
            var linkTarget = match.Groups[1].Value.Trim();
            var heading = match.Groups[2].Success ? match.Groups[2].Value : string.Empty;
            var displayText = match.Groups[3].Success ? match.Groups[3].Value.Trim() : null;

            // Convert link target to valid file path
            var filePath = MarkdownWikiSyntax.ToFilePath(linkTarget) + ".md";

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
    /// Convert a single link target to a valid file path
    /// </summary>
    public static string ConvertLinkTarget(string linkTarget)
    {
        return MarkdownWikiSyntax.ToFilePath(linkTarget);
    }
}
