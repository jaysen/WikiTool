using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace WikiTools;

public class WikidpadPage : LocalPage
{
    private WikidpadWiki _wiki;

    public WikidpadPage(string location, WikidpadWiki wiki = null) : base(location)
    {
        if (System.IO.Path.GetExtension(location) != ".wiki")
        {
            throw new FormatException("This is not a path to a .wiki page");
        }
        _wiki = wiki;
    }

    public override List<string> GetLinks()
    {
        if (ContentIsStale)
        {
            GetContent();
        }

        var links = new List<string>();
        var content = GetContent();

        // Use syntax pattern from parent wiki
        var syntax = (_wiki?.Syntax ?? new WikidPadSyntax()) as WikidPadSyntax;
        var matches = syntax.LinkPattern.Matches(content);

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

        // Use syntax patterns from parent wiki
        var syntax = (_wiki?.Syntax ?? new WikidPadSyntax()) as WikidPadSyntax;

        // Match WikidPad tags: [tag:tagname]
        var matches = syntax.TagPattern.Matches(content);

        foreach (Match match in matches)
        {
            var tag = match.Groups[1].Value.Trim();
            if (!tags.Contains(tag))
            {
                tags.Add(tag);
            }
        }

        // Match Category tags: CategoryTagName
        var categoryMatches = WikidPadSyntax.CategoryPattern.Matches(content);

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

        // Use syntax pattern from parent wiki
        var syntax = (_wiki?.Syntax ?? new WikidPadSyntax()) as WikidPadSyntax;
        var matches = syntax.HeaderPattern.Matches(content);

        foreach (Match match in matches)
        {
            headers.Add(match.Groups[2].Value.Trim());
        }

        return headers;
    }

    public override Dictionary<string, string> GetAttributes()
    {
        if (ContentIsStale)
        {
            GetContent();
        }

        var attributes = new Dictionary<string, string>();
        var content = GetContent();

        // Use syntax pattern from parent wiki
        var syntax = (_wiki?.Syntax ?? new WikidPadSyntax()) as WikidPadSyntax;
        var matches = syntax.AttributePattern.Matches(content);

        foreach (Match match in matches)
        {
            var key = match.Groups[1].Value.Trim();
            var value = match.Groups[2].Value.Trim();

            // Use key as-is (case-sensitive), but avoid duplicates by overwriting
            attributes[key] = value;
        }

        return attributes;
    }
}