using System.Text.RegularExpressions;

namespace WikiTool.Wikis;


/// <summary>
/// Syntax patterns for a plain Markdown wiki: standard inline links, ATX headers,
/// and metadata carried in YAML frontmatter. This is the format that publishes
/// directly to GitHub Pages.
/// All patterns are generated at compile-time for maximum performance
/// </summary>
public partial class MarkdownSyntax : WikiSyntax
{
    /// <summary>
    /// Default static instance for efficient fallback usage
    /// </summary>
    public static readonly MarkdownSyntax Default = new();

    /// <summary>
    /// Pattern for matching standard Markdown links: [display](target)
    /// Group 1 is the display text, group 2 is the link target.
    /// The lookbehind skips image embeds ![alt](src), which are not wiki links.
    /// Excludes newlines to prevent matching malformed patterns (missing closing bracket)
    /// </summary>
    [GeneratedRegex(@"(?<!!)\[([^\]\r\n]*)\]\(([^)\r\n]+)\)")]
    private static partial Regex LinkPatternRegex();
    public override Regex LinkPattern => LinkPatternRegex();

    /// <summary>
    /// Pattern for matching inline tags: #tagname, including nested tags such as #status/wip
    /// Matches after whitespace or at start of line. Requires a non-space directly after the
    /// hash so that ATX headers ("# Heading") are not mistaken for tags.
    /// </summary>
    [GeneratedRegex(@"(?:^|\s)#([a-zA-Z0-9_/-]+)", RegexOptions.Multiline)]
    private static partial Regex TagPatternRegex();
    public override Regex TagPattern => TagPatternRegex();

    /// <summary>
    /// Pattern for matching Markdown headers: #, ##, ###, etc.
    /// </summary>
    [GeneratedRegex(@"^(#{1,6})\s+(.+)$", RegexOptions.Multiline)]
    private static partial Regex HeaderPatternRegex();
    public override Regex HeaderPattern => HeaderPatternRegex();

    /// <summary>
    /// Pattern for matching an inline aliases array in YAML frontmatter: aliases: [a, b]
    /// Retained to satisfy the WikiSyntax contract. MarkdownPage reads aliases through
    /// <see cref="YamlFrontmatter"/> instead, which also understands the block-list form.
    /// </summary>
    [GeneratedRegex(@"(?:aliases|redirect_from):\s*\[([^\]\r\n]+)\]")]
    private static partial Regex AliasPatternRegex();
    public override Regex AliasPattern => AliasPatternRegex();

    /// <summary>
    /// Pattern for matching key-value pairs in YAML frontmatter: key: value
    /// A Markdown wiki has no inline attribute syntax, so frontmatter is the only source.
    /// </summary>
    [GeneratedRegex(@"^([a-zA-Z0-9_-]+):\s*(.+)$", RegexOptions.Multiline)]
    private static partial Regex AttributePatternRegex();
    public override Regex AttributePattern => AttributePatternRegex();
}
