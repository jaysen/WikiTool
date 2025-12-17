using System.Text.RegularExpressions;

namespace WikiTool.Wikis;

/// <summary>
/// Syntax patterns for standard Markdown wiki format (GitHub Pages compatible)
/// Uses standard markdown link syntax: [link-name](relative-path.md)
/// All patterns are generated at compile-time for maximum performance
/// </summary>
public partial class MarkdownWikiSyntax : WikiSyntax
{
    /// <summary>
    /// Default static instance for efficient fallback usage
    /// </summary>
    public static readonly MarkdownWikiSyntax Default = new();

    /// <summary>
    /// Pattern for matching standard markdown links: [text](url)
    /// Captures: Group 1 = link text, Group 2 = URL/path
    /// </summary>
    [GeneratedRegex(@"\[([^\]]+)\]\(([^)]+)\)")]
    private static partial Regex LinkPatternRegex();
    public override Regex LinkPattern => LinkPatternRegex();

    /// <summary>
    /// Pattern for matching inline tags: #tagname
    /// Matches after whitespace or at start of line
    /// </summary>
    [GeneratedRegex(@"(?:^|\s)#([a-zA-Z0-9_-]+)", RegexOptions.Multiline)]
    private static partial Regex TagPatternRegex();
    public override Regex TagPattern => TagPatternRegex();

    /// <summary>
    /// Pattern for matching YAML frontmatter
    /// </summary>
    [GeneratedRegex(@"^---\s*\n(.*?)\n---", RegexOptions.Singleline)]
    private static partial Regex YamlPatternRegex();
    public static Regex YamlPattern => YamlPatternRegex();

    /// <summary>
    /// Pattern for matching Markdown headers: #, ##, ###, etc.
    /// </summary>
    [GeneratedRegex(@"^(#+)\s+(.+)$", RegexOptions.Multiline)]
    private static partial Regex HeaderPatternRegex();
    public override Regex HeaderPattern => HeaderPatternRegex();

    /// <summary>
    /// Pattern for matching aliases in YAML frontmatter: aliases: [alias1, alias2]
    /// </summary>
    [GeneratedRegex(@"aliases:\s*\[([^\]\r\n]+)\]")]
    private static partial Regex AliasPatternRegex();
    public override Regex AliasPattern => AliasPatternRegex();

    /// <summary>
    /// Pattern for matching key-value attributes in YAML frontmatter: key: value
    /// </summary>
    [GeneratedRegex(@"^([a-zA-Z0-9_-]+):\s*(.+)$", RegexOptions.Multiline)]
    private static partial Regex AttributePatternRegex();
    public override Regex AttributePattern => AttributePatternRegex();

    /// <summary>
    /// Converts a page name to a valid file path (no spaces, lowercase)
    /// </summary>
    public static string ToFilePath(string pageName)
    {
        if (string.IsNullOrEmpty(pageName))
            return pageName;

        // Replace spaces with hyphens and convert to lowercase for URL-safe paths
        return pageName
            .Replace(" ", "-")
            .ToLowerInvariant();
    }

    /// <summary>
    /// Creates a markdown link from a page name
    /// Example: "My Page" -> [My Page](my-page.md)
    /// </summary>
    public static string CreateLink(string pageName, string displayText = null)
    {
        if (string.IsNullOrEmpty(pageName))
            return string.Empty;

        var filePath = ToFilePath(pageName) + ".md";
        var text = displayText ?? pageName;
        return $"[{text}]({filePath})";
    }
}
