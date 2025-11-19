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
        // IMPORTANT: Tags must be converted before links to prevent [tag:...] from being treated as links
        content = ConvertHeaders(content);
        content = ConvertTags(content);
        content = ConvertLinks(content);

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
    /// WikidPad syntax:
    ///   - Bare CamelCase words (WikiWord) are auto-linked
    ///   - [Single bracket] for links with spaces or non-CamelCase
    /// Obsidian syntax:
    ///   - [[Double brackets]] for all links
    /// </summary>
    private string ConvertLinks(string content)
    {
        // First: Convert [single bracket links] to [[double brackets]]
        // This handles [Link with Spaces] and [any other text]
        // Negative lookbehind (?<!\[) ensures we don't match [ in [[...]]
        // Negative lookahead (?!\]) ensures we don't match ] in ...]]
        var singleBracketPattern = @"(?<!\[)\[([^\]]+)\](?!\])";
        content = Regex.Replace(content, singleBracketPattern, match =>
        {
            var linkText = match.Groups[1].Value;
            return $"[[{linkText}]]";
        });

        // Second: Convert bare CamelCase WikiWords to [[WikiWord]]
        // A WikiWord in WikidPad must have mixed case (at least one uppercase and one lowercase)
        // and at least 2 case transitions (e.g., Ab -> C, or AB -> c -> D)
        // Examples: WikiWord, AbC, ABcd, AbcD, ABcD, AbCDe, WikiWord123
        // We need to match: letter sequences with at least one uppercase, one lowercase, and another different case
        // Negative lookbehind (?<!\[) prevents matching if preceded by [
        // Negative lookahead (?!\]) prevents matching if followed by ]
        // This pattern matches any word that has at least one lowercase letter followed by an uppercase,
        // OR at least two uppercase with a lowercase somewhere
        var camelCasePattern = @"(?<!\[)\b([A-Z]*[a-z]+[A-Z][A-Za-z0-9]*|[A-Z]{2,}[a-z][A-Za-z0-9]*)\b(?!\])";
        content = Regex.Replace(content, camelCasePattern, match =>
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
        var tagPattern = @"\[tag:([^\]]+)\]";
        content = Regex.Replace(content, tagPattern, match =>
        {
            var tagName = match.Groups[1].Value.Trim();
            // Replace spaces in tag names with hyphens for Obsidian compatibility
            tagName = tagName.Replace(" ", "-");
            return $"#{tagName} ";
        });

        // Convert CategoryTagName to #TagName
        var categoryPattern = @"\bCategory([A-Z][a-zA-Z0-9]*)\b";
        content = Regex.Replace(content, categoryPattern, match =>
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
}
