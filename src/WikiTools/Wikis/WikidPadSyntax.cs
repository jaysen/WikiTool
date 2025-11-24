using System.Text.RegularExpressions;

namespace WikiTools;

/// <summary>
/// Syntax patterns for WikidPad wiki format
/// All patterns are generated at compile-time for maximum performance
/// </summary>
public partial class WikidPadSyntax : WikiSyntax
{
    /// <summary>
    /// Pattern for matching WikidPad bracketed links: [link] or [Wiki Words]
    /// Note: WikidPad uses SINGLE brackets only (not double [[brackets]])
    /// For bare CamelCase WikiWords without brackets, use CamelCaseLinkPattern
    /// This pattern is the same as SingleBracketLinkPattern
    /// Excludes newlines to prevent matching malformed patterns (missing closing bracket)
    /// </summary>
    [GeneratedRegex(@"(?<!\[)\[([^\]\r\n]+)\](?!\])")]
    private static partial Regex LinkPatternRegex();
    public override Regex LinkPattern => LinkPatternRegex();

    /// <summary>
    /// Pattern for matching bare CamelCase WikiWords (for conversion)
    /// A WikiWord must START with uppercase and have at least one case transition
    /// Examples: WikiWord, AbC, ABcd, AbcD, ABcD, AbCDe, WikiWord123
    /// Does NOT match: aaBB, camelCase, iPhone (lowercase start)
    /// </summary>
    [GeneratedRegex(@"(?<!\[)\b([A-Z][a-z]+[A-Z][A-Za-z0-9]*|[A-Z]{2,}[a-z][A-Za-z0-9]*)\b(?!\])")]
    private static partial Regex CamelCaseLinkPatternRegex();
    public static Regex CamelCaseLinkPattern => CamelCaseLinkPatternRegex();

    /// <summary>
    /// Pattern for matching single-bracket links: [link text]
    /// Uses negative lookbehind/lookahead to avoid matching [[ or ]]
    /// Excludes content containing colons to avoid matching attributes [key: value]
    /// Excludes content containing equals to avoid matching special attributes [icon=date]
    /// Excludes newlines to prevent matching malformed patterns (missing closing bracket)
    /// </summary>
    [GeneratedRegex(@"(?<!\[)\[([^\]:=\r\n]+)\](?!\])")]
    private static partial Regex SingleBracketLinkPatternRegex();
    public static Regex SingleBracketLinkPattern => SingleBracketLinkPatternRegex();

    /// <summary>
    /// Pattern for matching WikidPad tags: [tag:tagname]
    /// Excludes newlines to prevent matching malformed patterns (missing closing bracket)
    /// </summary>
    [GeneratedRegex(@"\[tag:([^\]\r\n]+)\]")]
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
    /// Pattern for matching WikidPad alias attributes: [alias:AliasName]
    /// Captures the alias name
    /// Excludes newlines to prevent matching malformed patterns (missing closing bracket)
    /// </summary>
    [GeneratedRegex(@"\[alias:([^\]\r\n]+)\]")]
    private static partial Regex AliasPatternRegex();
    public override Regex AliasPattern => AliasPatternRegex();

    /// <summary>
    /// Pattern for matching WikidPad attributes: [key: value]
    /// Captures both the attribute name (key) and value
    /// Uses negative lookahead to avoid matching already-converted [key:: value]
    /// Excludes newlines to prevent matching malformed patterns (missing closing bracket)
    /// </summary>
    [GeneratedRegex(@"\[([a-zA-Z0-9_-]+):(?!:)\s*([^\]\r\n]+)\]")]
    private static partial Regex AttributePatternRegex();
    public override Regex AttributePattern => AttributePatternRegex();
}
