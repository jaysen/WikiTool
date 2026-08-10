using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace WikiTool.Wikis;

/// <summary>
/// A single top-level key in the frontmatter, along with the raw lines it occupied
/// so unrecognised keys can be passed through a conversion untouched.
/// </summary>
public class YamlEntry
{
    public string Key { get; set; }

    /// <summary>
    /// Value written on the same line as the key, if any (empty for block lists).
    /// </summary>
    public string InlineValue { get; set; }

    /// <summary>
    /// Items of a block list, i.e. the "- item" lines beneath the key.
    /// </summary>
    public List<string> Items { get; } = new List<string>();

    /// <summary>
    /// The verbatim source lines for this entry, key line included.
    /// </summary>
    public List<string> RawLines { get; } = new List<string>();
}

/// <summary>
/// Minimal reader for the YAML frontmatter block at the top of a Markdown page.
/// This is deliberately not a general YAML parser - it understands the handful of
/// shapes wikis actually emit, in both the inline-array and block-list forms:
///
///   tags: [a, b]
///   tags:
///     - a
///     - b
///   tags: a
/// </summary>
public partial class YamlFrontmatter
{
    /// <summary>
    /// Matches the leading frontmatter block. Anchored at the start of the content.
    /// </summary>
    [GeneratedRegex(@"\A---[ \t]*\r?\n(.*?)\r?\n---[ \t]*(?:\r?\n|\z)", RegexOptions.Singleline)]
    private static partial Regex BlockRegex();

    /// <summary>
    /// Matches a top-level "key:" line (no leading indentation).
    /// </summary>
    [GeneratedRegex(@"^([A-Za-z0-9_.\-]+):[ \t]*(.*)$")]
    private static partial Regex KeyLineRegex();

    /// <summary>
    /// Matches a block-list item line: "  - value".
    /// </summary>
    [GeneratedRegex(@"^[ \t]*-[ \t]*(.*)$")]
    private static partial Regex ListItemRegex();

    /// <summary>
    /// True when the content actually began with a frontmatter block.
    /// </summary>
    public bool HasFrontmatter { get; private set; }

    /// <summary>
    /// The content with the frontmatter block removed.
    /// </summary>
    public string Body { get; private set; }

    /// <summary>
    /// Top-level keys, in the order they appeared.
    /// </summary>
    public List<YamlEntry> Entries { get; } = new List<YamlEntry>();

    private YamlFrontmatter() { }

    /// <summary>
    /// Split a page into its frontmatter entries and its body.
    /// Content without frontmatter yields an empty entry list and an unchanged body.
    /// </summary>
    public static YamlFrontmatter Parse(string content)
    {
        var result = new YamlFrontmatter { Body = content ?? string.Empty };

        if (string.IsNullOrEmpty(content))
        {
            return result;
        }

        var match = BlockRegex().Match(content);
        if (!match.Success)
        {
            return result;
        }

        result.HasFrontmatter = true;
        result.Body = content.Substring(match.Length);
        result.ReadEntries(match.Groups[1].Value);

        return result;
    }

    private void ReadEntries(string yaml)
    {
        var lines = yaml.Split('\n');
        YamlEntry current = null;

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd('\r');

            var keyMatch = KeyLineRegex().Match(line);
            if (keyMatch.Success)
            {
                current = new YamlEntry
                {
                    Key = keyMatch.Groups[1].Value,
                    InlineValue = keyMatch.Groups[2].Value.Trim()
                };
                current.RawLines.Add(line);
                Entries.Add(current);
                continue;
            }

            if (current == null)
            {
                continue;
            }

            current.RawLines.Add(line);

            var itemMatch = ListItemRegex().Match(line);
            if (itemMatch.Success)
            {
                var item = Clean(itemMatch.Groups[1].Value);
                if (item.Length > 0)
                {
                    current.Items.Add(item);
                }
            }
        }
    }

    /// <summary>
    /// Look up a top-level key, case-insensitively.
    /// </summary>
    public YamlEntry Find(string key)
    {
        foreach (var entry in Entries)
        {
            if (string.Equals(entry.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                return entry;
            }
        }

        return null;
    }

    /// <summary>
    /// Read a key as a list, accepting every shape a wiki might have written it in:
    /// an inline array, a block list, or a bare scalar.
    /// </summary>
    public List<string> GetList(string key)
    {
        var values = new List<string>();
        var entry = Find(key);

        if (entry == null)
        {
            return values;
        }

        // Block list: "- item" lines beneath the key
        if (entry.Items.Count > 0)
        {
            values.AddRange(entry.Items);
            return values;
        }

        var inline = entry.InlineValue;
        if (string.IsNullOrWhiteSpace(inline))
        {
            return values;
        }

        // Inline array: [a, b]
        if (inline.StartsWith("[") && inline.EndsWith("]"))
        {
            inline = inline.Substring(1, inline.Length - 2);
            foreach (var part in inline.Split(','))
            {
                var value = Clean(part);
                if (value.Length > 0)
                {
                    values.Add(value);
                }
            }

            return values;
        }

        // Bare scalar
        var scalar = Clean(inline);
        if (scalar.Length > 0)
        {
            values.Add(scalar);
        }

        return values;
    }

    /// <summary>
    /// Read a key as a single scalar value, or null when absent.
    /// </summary>
    public string GetScalar(string key)
    {
        var entry = Find(key);
        if (entry == null || string.IsNullOrWhiteSpace(entry.InlineValue))
        {
            return null;
        }

        return Clean(entry.InlineValue);
    }

    private static string Clean(string value)
    {
        return value.Trim().Trim('"', '\'').Trim();
    }
}
