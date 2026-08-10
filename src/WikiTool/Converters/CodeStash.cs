using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace WikiTool.Converters;

/// <summary>
/// Holds code blocks and code spans back while the rest of a page is rewritten,
/// then puts them back untouched.
///
/// Without this, a page documenting Obsidian syntax would have the [[links]] and #tags
/// inside its own code samples silently converted.
/// </summary>
internal partial class CodeStash
{
    /// <summary>
    /// Delimiter for stashed-code placeholders. A NUL cannot occur in Markdown source,
    /// so no rewriting rule can accidentally match inside a placeholder.
    /// </summary>
    private const char Marker = (char)0;

    /// <summary>
    /// Matches a fenced code block, with either backtick or tilde fences.
    /// </summary>
    [GeneratedRegex(@"^[ \t]*(`{3,}|~{3,})[^\r\n]*\r?\n[\s\S]*?^[ \t]*\1[ \t]*$", RegexOptions.Multiline)]
    private static partial Regex FencedCodeRegex();

    /// <summary>
    /// Matches an inline code span. Spans containing a literal backtick are not handled.
    /// </summary>
    [GeneratedRegex(@"`[^`\r\n]*`")]
    private static partial Regex InlineCodeRegex();

    /// <summary>
    /// Matches a placeholder previously written by <see cref="Hide"/>.
    /// </summary>
    [GeneratedRegex(@"\x00(\d+)\x00")]
    private static partial Regex PlaceholderRegex();

    private readonly List<string> _stashed = new List<string>();

    /// <summary>
    /// Replace every code block and code span with an opaque placeholder.
    /// Fenced blocks go first so that backticks inside them are not seen as code spans.
    /// </summary>
    public string Hide(string body)
    {
        body = FencedCodeRegex().Replace(body, Stash);
        body = InlineCodeRegex().Replace(body, Stash);
        return body;
    }

    /// <summary>
    /// Put the original code back in place of the placeholders.
    /// </summary>
    public string Restore(string body)
    {
        return PlaceholderRegex().Replace(body, match =>
        {
            var index = int.Parse(match.Groups[1].Value);
            return _stashed[index];
        });
    }

    private string Stash(Match match)
    {
        _stashed.Add(match.Value);
        return string.Concat(Marker, (_stashed.Count - 1).ToString(), Marker);
    }
}
