using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace WikiTools.Converters;

public class WikidPadToObsidianConverter
{
    public string SourcePath { get; }
    public string DestinationPath { get; }

    private readonly WikidpadWiki _sourceWiki;
    private readonly WikidPadSyntax _sourceSyntax;

    public WikidPadToObsidianConverter(string sourcePath, string destinationPath)
    {
        if (!Directory.Exists(sourcePath))
        {
            throw new DirectoryNotFoundException($"Source directory not found: {sourcePath}");
        }

        SourcePath = sourcePath;
        DestinationPath = destinationPath;

        _sourceWiki = new WikidpadWiki(sourcePath);
        _sourceSyntax = _sourceWiki.Syntax as WikidPadSyntax;
    }

    /// <summary>
    /// Convert all WikidPad wiki files to Obsidian format
    /// </summary>
    public void ConvertAll()
    {
        // Create destination directory if it doesn't exist
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
    /// Convert a single WikidPad page to Obsidian format
    /// </summary>
    private void ConvertPage(Page page)
    {
        var content = page.GetContent();
        var convertedContent = ConvertContent(content);

        // Create output file path (.wiki -> .md)
        var fileName = page.Name + ".md";
        var outputPath = Path.Combine(DestinationPath, fileName);

        File.WriteAllText(outputPath, convertedContent);
    }

    /// <summary>
    /// Convert WikidPad content to Obsidian markdown format
    /// </summary>
    public string ConvertContent(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return content;
        }

        // Apply conversions in order
        // IMPORTANT: Tags and attributes must be converted before links to prevent [tag:...] and [attr:...] from being treated as links
        content = ConvertHeaders(content);
        content = ConvertTags(content);
        content = ConvertAttributes(content);
        content = ConvertLinks(content);

        return content;
    }

    /// <summary>
    /// Convert WikidPad headers (+, ++, +++) to Markdown headers (#, ##, ###)
    /// </summary>
    private string ConvertHeaders(string content)
    {
        // Use WikidPad syntax pattern for headers
        return _sourceSyntax.HeaderPattern.Replace(content, match =>
        {
            var plusCount = match.Groups[1].Value.Length;
            var headerText = match.Groups[2].Value.Trim();
            var hashes = new string('#', plusCount);

            return $"{hashes} {headerText}";
        });
    }

    /// <summary>
    /// Convert WikidPad links to Obsidian wikilinks
    /// WikidPad syntax:
    ///   - Bare CamelCase words (WikiWord) are auto-linked
    ///   - [Single bracket] for links with spaces or non-CamelCase
    /// Obsidian syntax:
    ///   - [[Double brackets]] for all links
    /// </summary>
    private string ConvertLinks(string content)
    {
        // First: Convert [single bracket links] to [[double brackets]]
        // Use WikidPad syntax pattern for single bracket links
        content = WikidPadSyntax.SingleBracketLinkPattern.Replace(content, match =>
        {
            var linkText = match.Groups[1].Value;
            return $"[[{linkText}]]";
        });

        // Second: Convert bare CamelCase WikiWords to [[WikiWord]]
        // Use WikidPad syntax pattern for CamelCase links
        content = WikidPadSyntax.CamelCaseLinkPattern.Replace(content, match =>
        {
            return $"[[{match.Value}]]";
        });

        return content;
    }

    /// <summary>
    /// Convert WikidPad tags to Obsidian inline tags
    /// </summary>
    private string ConvertTags(string content)
    {
        // Convert [tag:tagname] to #tagname with space after
        // Use WikidPad syntax pattern for tags
        content = _sourceSyntax.TagPattern.Replace(content, match =>
        {
            var tagName = match.Groups[1].Value.Trim();
            // Replace spaces in tag names with hyphens for Obsidian compatibility
            tagName = tagName.Replace(" ", "-");
            return $"#{tagName} ";
        });

        // Convert CategoryTagName to #TagName
        // Use WikidPad syntax pattern for category tags
        content = WikidPadSyntax.CategoryPattern.Replace(content, match =>
        {
            var tagName = match.Groups[1].Value;
            return $"#{tagName}";
        });

        // Clean up any double spaces or trailing spaces at end of lines
        content = Regex.Replace(content, @" +", " "); // Replace multiple spaces with single space
        content = Regex.Replace(content, @" +(\r?\n)", "$1"); // Remove trailing spaces before newlines
        content = Regex.Replace(content, @" +$", ""); // Remove trailing spaces at end of content

        return content;
    }

    /// <summary>
    /// Convert WikidPad attributes to Obsidian inline attributes
    /// WikidPad syntax: [key: value]
    /// Obsidian syntax: [key:: value]
    /// </summary>
    private string ConvertAttributes(string content)
    {
        // Convert [key: value] to [key:: value]
        // Use WikidPad syntax pattern for attributes
        return _sourceSyntax.AttributePattern.Replace(content, match =>
        {
            var key = match.Groups[1].Value.Trim();
            var value = match.Groups[2].Value.Trim();
            return $"[{key}:: {value}]";
        });
    }
}
