using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace WikiTool.Converters;

public class WikidPadToObsidianConverter
{
    public string SourcePath { get; }
    public string DestinationPath { get; }

    /// <summary>
    /// When true, converts WikidPad CategoryName patterns to #Name tags.
    /// Default is false (CategoryName is left unchanged).
    /// </summary>
    public bool ConvertCategoryTags { get; set; } = false;

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

        // Extract and preserve existing YAML frontmatter
        string existingFrontmatter = null;
        var yamlMatch = Regex.Match(content, @"^---\r?\n(.*?)\r?\n---\r?\n", RegexOptions.Singleline);
        if (yamlMatch.Success)
        {
            existingFrontmatter = yamlMatch.Value;
            content = content.Substring(yamlMatch.Length);
        }

        // Extract aliases first (they need to go in YAML frontmatter)
        var aliases = ExtractAliases(content);
        content = RemoveAliases(content);

        // Apply conversions in order
        // IMPORTANT: Tags and attributes must be converted before links to prevent [tag:...] and [attr:...] from being treated as links
        content = ConvertHeaders(content);
        content = ConvertTags(content);
        content = ConvertAttributes(content);
        content = ConvertLinks(content);

        // Restore existing frontmatter if it was present
        if (existingFrontmatter != null)
        {
            content = existingFrontmatter + content;
        }

        // Add or merge aliases into YAML frontmatter if we have aliases
        if (aliases.Count > 0)
        {
            content = AddOrMergeYamlFrontmatter(content, aliases);
        }

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

        // Convert CategoryTagName to #TagName (only if enabled)
        if (ConvertCategoryTags)
        {
            content = WikidPadSyntax.CategoryPattern.Replace(content, match =>
            {
                var tagName = match.Groups[1].Value;
                return $"#{tagName}";
            });
        }

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

    /// <summary>
    /// Extract all aliases from WikidPad content
    /// WikidPad syntax: [alias:AliasName]
    /// </summary>
    private List<string> ExtractAliases(string content)
    {
        var aliases = new List<string>();
        var matches = _sourceSyntax.AliasPattern.Matches(content);

        foreach (Match match in matches)
        {
            var alias = match.Groups[1].Value.Trim();
            if (!string.IsNullOrEmpty(alias))
            {
                aliases.Add(alias);
            }
        }

        return aliases;
    }

    /// <summary>
    /// Remove all alias tags from content
    /// </summary>
    private string RemoveAliases(string content)
    {
        // Remove [alias:...] tags and any trailing whitespace/newlines
        content = _sourceSyntax.AliasPattern.Replace(content, "");
        // Clean up any leftover empty lines at the start
        content = Regex.Replace(content, @"^\s*\n", "", RegexOptions.Multiline);
        return content.TrimStart();
    }

    /// <summary>
    /// Add or merge aliases into YAML frontmatter
    /// </summary>
    private static string AddOrMergeYamlFrontmatter(string content, List<string> aliases)
    {
        if (aliases.Count == 0)
        {
            return content;
        }

        // Check if content already has YAML frontmatter
        var yamlMatch = Regex.Match(content, @"^---\r?\n(.*?)\r?\n---\r?\n", RegexOptions.Singleline);

        if (yamlMatch.Success)
        {
            // Content has existing frontmatter - merge aliases into it
            var existingYaml = yamlMatch.Groups[1].Value;
            var contentAfterYaml = content.Substring(yamlMatch.Length);

            // Check if aliases already exist in frontmatter
            var aliasesMatch = Regex.Match(existingYaml, @"^aliases:\s*$", RegexOptions.Multiline);

            if (aliasesMatch.Success)
            {
                // Aliases section exists - append to it
                // Find where the aliases section ends (next top-level key or end of YAML)
                var aliasesEndMatch = Regex.Match(existingYaml.Substring(aliasesMatch.Index + aliasesMatch.Length),
                    @"^[a-zA-Z]", RegexOptions.Multiline);

                var insertPosition = aliasesMatch.Index + aliasesMatch.Length;
                if (aliasesEndMatch.Success)
                {
                    insertPosition += aliasesEndMatch.Index;
                }
                else
                {
                    insertPosition = existingYaml.Length;
                }

                var sb = new StringBuilder();
                foreach (var alias in aliases)
                {
                    sb.AppendLine($"  - {alias}");
                }

                var updatedYaml = existingYaml.Insert(insertPosition, sb.ToString());
                return $"---\n{updatedYaml}\n---\n{contentAfterYaml}";
            }
            else
            {
                // No aliases section - add it to the end of existing frontmatter
                var sb = new StringBuilder();
                sb.AppendLine(existingYaml.TrimEnd());
                sb.AppendLine("aliases:");
                foreach (var alias in aliases)
                {
                    sb.AppendLine($"  - {alias}");
                }
                return $"---\n{sb}---\n{contentAfterYaml}";
            }
        }
        else
        {
            // No existing frontmatter - create new one
            return GenerateYamlFrontmatter(aliases) + content;
        }
    }

    /// <summary>
    /// Generate YAML frontmatter with aliases
    /// </summary>
    private static string GenerateYamlFrontmatter(List<string> aliases)
    {
        var sb = new StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine("aliases:");
        foreach (var alias in aliases)
        {
            sb.AppendLine($"  - {alias}");
        }
        sb.AppendLine("---");
        return sb.ToString();
    }
}
