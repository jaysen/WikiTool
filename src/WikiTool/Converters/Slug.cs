using System.Globalization;
using System.Text;

namespace WikiTool.Converters;

/// <summary>
/// Generates URL-safe slugs for output filenames and heading anchors.
/// </summary>
public static class Slug
{
    /// <summary>
    /// Slugify a page or folder name for use as an output filename.
    /// Lowercases, strips diacritics, and reduces everything else to hyphens.
    /// Never returns a value starting with '_' or '.', which Jekyll silently excludes from the site.
    /// </summary>
    public static string ForFile(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "page";
        }

        // Expand language names that would otherwise collapse away entirely (C#, C++)
        value = value.Replace("#", "-sharp-").Replace("+", "-plus-");

        var sb = new StringBuilder(value.Length);
        foreach (var c in StripDiacritics(value).ToLowerInvariant())
        {
            sb.Append(char.IsLetterOrDigit(c) ? c : '-');
        }

        var slug = CollapseHyphens(sb.ToString());

        // Empty, or reduced to nothing but separators
        return slug.Length == 0 ? "page" : slug;
    }

    /// <summary>
    /// Slugify every segment of a wiki-relative path, preserving the folder structure.
    /// Input and output both use '/' separators.
    /// </summary>
    public static string ForPath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return string.Empty;
        }

        var segments = relativePath.Replace('\\', '/').Split('/');
        for (var i = 0; i < segments.Length; i++)
        {
            segments[i] = ForFile(segments[i]);
        }

        return string.Join("/", segments);
    }

    /// <summary>
    /// Generate the anchor kramdown (the GitHub Pages Markdown engine) will assign to a heading,
    /// so that [[Page#Heading]] links land on the right place in the built site.
    /// </summary>
    public static string ForAnchor(string heading)
    {
        if (string.IsNullOrWhiteSpace(heading))
        {
            return string.Empty;
        }

        var sb = new StringBuilder(heading.Length);
        foreach (var c in StripDiacritics(heading).ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c))
            {
                sb.Append(c);
            }
            else if (c == ' ' || c == '-' || c == '_')
            {
                sb.Append('-');
            }
            // All other punctuation is dropped rather than replaced, matching kramdown
        }

        var anchor = CollapseHyphens(sb.ToString());

        // kramdown prefixes ids that do not start with a letter
        if (anchor.Length > 0 && !char.IsLetter(anchor[0]))
        {
            anchor = "section-" + anchor;
        }

        return anchor;
    }

    private static string StripDiacritics(string value)
    {
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(decomposed.Length);

        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// Collapse runs of hyphens into one and trim them from both ends.
    /// </summary>
    private static string CollapseHyphens(string value)
    {
        var sb = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            if (c == '-' && (sb.Length == 0 || sb[sb.Length - 1] == '-'))
            {
                continue;
            }
            sb.Append(c);
        }

        while (sb.Length > 0 && sb[sb.Length - 1] == '-')
        {
            sb.Length--;
        }

        return sb.ToString();
    }
}
