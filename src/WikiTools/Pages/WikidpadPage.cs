using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace WikiTools;

public class WikidpadPage : LocalPage
{
    public WikidpadPage(string location) : base(location)
    {
        if (System.IO.Path.GetExtension(location) != ".wiki")
        {
            throw new FormatException("This is not a path to a .wiki page");
        }
    }

    public override List<string> GetLinks()
    {
        if (ContentIsStale)
        {
            GetContent();
        }

        var links = new List<string>();
        var content = GetContent();

        // Match WikidPad links: [WikiWord] or [[link with spaces]]
        // WikiWords (CamelCase words)
        var wikiWordPattern = @"\[\[([^\]]+)\]\]|\[([A-Z][a-z]+(?:[A-Z][a-z]+)+)\]";
        var matches = Regex.Matches(content, wikiWordPattern);

        foreach (Match match in matches)
        {
            var link = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
            if (!string.IsNullOrEmpty(link))
            {
                links.Add(link);
            }
        }

        return links;
    }

    public override List<string> GetAliases()
    {
        // WikidPad doesn't have a standard alias format like Obsidian
        // Return empty list for now
        return new List<string>();
    }

    public override List<string> GetTags()
    {
        if (ContentIsStale)
        {
            GetContent();
        }

        var tags = new List<string>();
        var content = GetContent();

        // Match WikidPad tags: [tag:tagname]
        var tagPattern = @"\[tag:([^\]]+)\]";
        var matches = Regex.Matches(content, tagPattern);

        foreach (Match match in matches)
        {
            var tag = match.Groups[1].Value.Trim();
            if (!tags.Contains(tag))
            {
                tags.Add(tag);
            }
        }

        // Match Category tags: CategoryTagName (words starting with "Category")
        var categoryPattern = @"\bCategory([A-Z][a-zA-Z0-9]*)\b";
        var categoryMatches = Regex.Matches(content, categoryPattern);

        foreach (Match match in categoryMatches)
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
        var content = GetContent();

        // Match WikidPad headers: lines starting with +, ++, +++, etc.
        var headerPattern = @"^\+{1,}\s*(.+)$";
        var matches = Regex.Matches(content, headerPattern, RegexOptions.Multiline);

        foreach (Match match in matches)
        {
            headers.Add(match.Groups[1].Value.Trim());
        }

        return headers;
    }
}