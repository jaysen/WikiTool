using System.Text.RegularExpressions;

namespace WikiTools;

/// <summary>
/// Syntax patterns for Obsidian markdown format
/// All patterns are generated at compile-time for maximum performance
/// </summary>
public partial class ObsidianSyntax : WikiSyntax
{
    /// <summary>
    /// Pattern for matching Obsidian wikilinks: [[link]] or [[link|display]]
    /// </summary>
    [GeneratedRegex(@"\[\[([^\]|]+)(?:\|[^\]]*)?\]\]")]
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
    /// Pattern for matching tags in YAML frontmatter: tags: [tag1, tag2]
    /// </summary>
    [GeneratedRegex(@"tags:\s*\[([^\]]+)\]")]
    private static partial Regex YamlTagPatternRegex();
    public static Regex YamlTagPattern => YamlTagPatternRegex();

    /// <summary>
    /// Pattern for matching aliases in YAML frontmatter: aliases: [alias1, alias2]
    /// </summary>
    [GeneratedRegex(@"aliases:\s*\[([^\]]+)\]")]
    private static partial Regex AliasPatternRegex();
    public override Regex AliasPattern => AliasPatternRegex();

    /// <summary>
    /// Pattern for matching Markdown headers: #, ##, ###, etc.
    /// </summary>
    [GeneratedRegex(@"^(#+)\s+(.+)$", RegexOptions.Multiline)]
    private static partial Regex HeaderPatternRegex();
    public override Regex HeaderPattern => HeaderPatternRegex();

    /// <summary>
    /// Pattern for matching inline Obsidian/Dataview attributes: [key:: value]
    /// Captures both the attribute name (key) and value
    /// </summary>
    [GeneratedRegex(@"\[([a-zA-Z0-9_-]+)::\s*([^\]]+)\]")]
    private static partial Regex AttributePatternRegex();
    public override Regex AttributePattern => AttributePatternRegex();

    /// <summary>
    /// Pattern for matching key-value pairs in YAML frontmatter: key: value
    /// Matches a line with a key followed by colon and value
    /// </summary>
    [GeneratedRegex(@"^([a-zA-Z0-9_-]+):\s*(.+)$", RegexOptions.Multiline)]
    private static partial Regex YamlAttributePatternRegex();
    public static Regex YamlAttributePattern => YamlAttributePatternRegex();
}
