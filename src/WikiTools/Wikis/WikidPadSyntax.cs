using System.Text.RegularExpressions;

namespace WikiTools;

/// <summary>
/// Syntax patterns for WikidPad wiki format
/// All patterns are generated at compile-time for maximum performance
/// </summary>
public partial class WikidPadSyntax : WikiSyntax
{
    /// <summary>
    /// Pattern for matching WikidPad links: [[link with spaces]] or [WikiWord]
    /// Note: This matches both double-bracket and single-bracket CamelCase links
    /// </summary>
    [GeneratedRegex(@"\[\[([^\]]+)\]\]|\[([A-Z][a-z]+(?:[A-Z][a-z]+)+)\]")]
    private static partial Regex LinkPatternRegex();
    public override Regex LinkPattern => LinkPatternRegex();

    /// <summary>
    /// Pattern for matching bare CamelCase WikiWords (for conversion)
    /// A WikiWord must have mixed case with at least 2 case transitions
    /// Examples: WikiWord, AbC, ABcd, AbcD, ABcD, AbCDe, WikiWord123
    /// </summary>
    [GeneratedRegex(@"(?<!\[)\b([A-Z]*[a-z]+[A-Z][A-Za-z0-9]*|[A-Z]{2,}[a-z][A-Za-z0-9]*)\b(?!\])")]
    private static partial Regex CamelCaseLinkPatternRegex();
    public static Regex CamelCaseLinkPattern => CamelCaseLinkPatternRegex();

    /// <summary>
    /// Pattern for matching single-bracket links: [link text]
    /// Uses negative lookbehind/lookahead to avoid matching [[ or ]]
    /// </summary>
    [GeneratedRegex(@"(?<!\[)\[([^\]]+)\](?!\])")]
    private static partial Regex SingleBracketLinkPatternRegex();
    public static Regex SingleBracketLinkPattern => SingleBracketLinkPatternRegex();

    /// <summary>
    /// Pattern for matching WikidPad tags: [tag:tagname]
    /// </summary>
    [GeneratedRegex(@"\[tag:([^\]]+)\]")]
    private static partial Regex TagPatternRegex();
    public override Regex TagPattern => TagPatternRegex();

    /// <summary>
    /// Pattern for matching WikidPad Category tags: CategoryTagName
    /// </summary>
    [GeneratedRegex(@"\bCategory([A-Z][a-zA-Z0-9]*)\b")]
    private static partial Regex CategoryPatternRegex();
    public static Regex CategoryPattern => CategoryPatternRegex();

    /// <summary>
    /// Pattern for matching WikidPad headers: +, ++, +++, etc.
    /// </summary>
    [GeneratedRegex(@"^(\+{1,})\s*(.+)$", RegexOptions.Multiline)]
    private static partial Regex HeaderPatternRegex();
    public override Regex HeaderPattern => HeaderPatternRegex();

    /// <summary>
    /// WikidPad doesn't support aliases
    /// </summary>
    public override Regex AliasPattern => null;

    /// <summary>
    /// Pattern for matching WikidPad attributes: [key: value]
    /// Captures both the attribute name (key) and value
    /// </summary>
    [GeneratedRegex(@"\[([a-zA-Z0-9_-]+):\s*([^\]]+)\]")]
    private static partial Regex AttributePatternRegex();
    public override Regex AttributePattern => AttributePatternRegex();
}
