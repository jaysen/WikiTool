using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using WikiTool.Wikis;

namespace WikiTool.Pages;

public class MarkdownPage : LocalPage
{
    private MarkdownWiki _wiki;

    public MarkdownPage(string location, MarkdownWiki wiki = null) : base(location)
    {
        var ext = System.IO.Path.GetExtension(location);
        if (ext != ".md")
        {
            throw new FormatException("This is not a path to a .md page");
        }
        _wiki = wiki;
    }

    private MarkdownSyntax GetSyntax()
    {
        return (_wiki?.Syntax as MarkdownSyntax) ?? MarkdownSyntax.Default;
    }

    /// <summary>
    /// Returns the link targets of every standard Markdown link on the page.
    /// Unlike ObsidianPage, which returns page names, these are paths relative to this page.
    /// </summary>
    public override List<string> GetLinks()
    {
        if (ContentIsStale)
        {
            GetContent();
        }

        var links = new List<string>();
        var content = GetContent();

        var matches = GetSyntax().LinkPattern.Matches(content);

        foreach (Match match in matches)
        {
            links.Add(match.Groups[2].Value);
        }

        return links;
    }

    /// <summary>
    /// Aliases come from frontmatter. Both "aliases" and the Jekyll "redirect_from"
    /// key are read, since the Obsidian conversion writes alternate names as redirects.
    /// </summary>
    public override List<string> GetAliases()
    {
        if (ContentIsStale)
        {
            GetContent();
        }

        var frontmatter = YamlFrontmatter.Parse(GetContent());
        var aliases = new List<string>();

        foreach (var key in new[] { "aliases", "redirect_from" })
        {
            foreach (var alias in frontmatter.GetList(key))
            {
                if (!aliases.Contains(alias))
                {
                    aliases.Add(alias);
                }
            }
        }

        return aliases;
    }

    public override List<string> GetTags()
    {
        if (ContentIsStale)
        {
            GetContent();
        }

        var content = GetContent();
        var frontmatter = YamlFrontmatter.Parse(content);
        var tags = new List<string>();

        foreach (var tag in frontmatter.GetList("tags"))
        {
            if (!tags.Contains(tag))
            {
                tags.Add(tag);
            }
        }

        // Inline #tags may still be present in a hand-written page
        var matches = GetSyntax().TagPattern.Matches(frontmatter.Body);

        foreach (Match match in matches)
        {
            var tag = match.Groups[1].Value;
            if (!tags.Contains(tag))
            {
                tags.Add(tag);
            }
        }

        return tags;
    }

    public override List<string> GetHeaders()
    {
        if (ContentIsStale)
        {
            GetContent();
        }

        var headers = new List<string>();

        // Parse the body so a "---" frontmatter fence is never read as a header
        var frontmatter = YamlFrontmatter.Parse(GetContent());
        var matches = GetSyntax().HeaderPattern.Matches(frontmatter.Body);

        foreach (Match match in matches)
        {
            headers.Add(match.Groups[2].Value.Trim());
        }

        return headers;
    }

    /// <summary>
    /// A Markdown wiki carries no inline attribute syntax, so every attribute
    /// comes from the YAML frontmatter.
    /// </summary>
    public override Dictionary<string, string> GetAttributes()
    {
        if (ContentIsStale)
        {
            GetContent();
        }

        var attributes = new Dictionary<string, string>();
        var frontmatter = YamlFrontmatter.Parse(GetContent());

        foreach (var entry in frontmatter.Entries)
        {
            // Tags and aliases are exposed through GetTags/GetAliases
            if (entry.Key == "tags" || entry.Key == "aliases" || entry.Key == "redirect_from")
            {
                continue;
            }

            var value = entry.Items.Count > 0
                ? string.Join(", ", entry.Items)
                : entry.InlineValue;

            if (value != null && value.StartsWith("[") && value.EndsWith("]"))
            {
                value = value.Trim('[', ']').Trim();
            }

            attributes[entry.Key] = (value ?? string.Empty).Trim('"', '\'');
        }

        return attributes;
    }
}
