using System.Text.RegularExpressions;

namespace WikiTools;

/// <summary>
/// Defines the syntax patterns for a wiki format.
/// Each wiki format (WikidPad, Obsidian, etc.) implements this to provide
/// compiled regex patterns for identifying and parsing wiki-specific elements.
/// </summary>
public abstract class WikiSyntax
{
    /// <summary>
    /// Pattern for matching wiki links
    /// </summary>
    public abstract Regex LinkPattern { get; }

    /// <summary>
    /// Pattern for matching tags
    /// </summary>
    public abstract Regex TagPattern { get; }

    /// <summary>
    /// Pattern for matching headers
    /// </summary>
    public abstract Regex HeaderPattern { get; }

    /// <summary>
    /// Pattern for matching aliases (if supported by the wiki format)
    /// Returns null if the format doesn't support aliases
    /// </summary>
    public abstract Regex AliasPattern { get; }

    /// <summary>
    /// Pattern for matching attributes (key-value pairs)
    /// Returns null if the format doesn't support attributes
    /// </summary>
    public abstract Regex AttributePattern { get; }
}
