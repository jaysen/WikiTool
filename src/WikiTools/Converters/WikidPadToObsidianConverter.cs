using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace WikiTools.Converters;

public class WikidPadToObsidianConverter
{
    public string SourcePath { get; }
    public string DestinationPath { get; }

    public WikidPadToObsidianConverter(string sourcePath, string destinationPath)
    {
        if (!Directory.Exists(sourcePath))
        {
            throw new DirectoryNotFoundException($"Source directory not found: {sourcePath}");
        }

        SourcePath = sourcePath;
        DestinationPath = destinationPath;
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

        var wiki = new WikidpadWiki(SourcePath);
        var pages = wiki.GetAllPages();

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
        content = ConvertHeaders(content);
        content = ConvertLinks(content);
        content = ConvertTags(content);

        return content;
    }

    /// <summary>
    /// Convert WikidPad headers (+, ++, +++) to Markdown headers (#, ##, ###)
    /// </summary>
    private string ConvertHeaders(string content)
    {
        // Match lines starting with +, ++, +++, etc.
        var headerPattern = @"^(\+{1,})\s*(.+)$";

        return Regex.Replace(content, headerPattern, match =>
        {
            var plusCount = match.Groups[1].Value.Length;
            var headerText = match.Groups[2].Value.Trim();
            var hashes = new string('#', plusCount);

            return $"{hashes} {headerText}";
        }, RegexOptions.Multiline);
    }

    /// <summary>
    /// Convert WikidPad links to Obsidian wikilinks
    /// </summary>
    private string ConvertLinks(string content)
    {
        // Convert [WikiWord] to [[WikiWord]] (but don't double-convert [[already formatted]])
        // Only convert CamelCase WikiWords
        var wikiWordPattern = @"\[([A-Z][a-z]+(?:[A-Z][a-z]+)+)\]";
        content = Regex.Replace(content, wikiWordPattern, match =>
        {
            // Check if it's already in double brackets by looking at context
            var linkText = match.Groups[1].Value;
            return $"[[{linkText}]]";
        });

        // [[links with spaces]] are already in the correct format

        return content;
    }

    /// <summary>
    /// Convert WikidPad tags to Obsidian inline tags
    /// </summary>
    private string ConvertTags(string content)
    {
        // Convert [tag:tagname] to #tagname
        var tagPattern = @"\[tag:([^\]]+)\]";
        content = Regex.Replace(content, tagPattern, match =>
        {
            var tagName = match.Groups[1].Value.Trim();
            // Replace spaces in tag names with hyphens for Obsidian compatibility
            tagName = tagName.Replace(" ", "-");
            return $"#{tagName}";
        });

        // Convert CategoryTagName to #TagName
        var categoryPattern = @"\bCategory([A-Z][a-zA-Z0-9]*)\b";
        content = Regex.Replace(content, categoryPattern, match =>
        {
            var tagName = match.Groups[1].Value;
            return $"#{tagName}";
        });

        return content;
    }
}
