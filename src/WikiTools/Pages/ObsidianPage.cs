using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace WikiTools;

public class ObsidianPage : LocalPage
{
    public ObsidianPage(string location) : base(location)
    {
        var ext = System.IO.Path.GetExtension(location);
        if (ext != ".md")
        {
            throw new FormatException("This is not a path to a .md page");
        }
    }

    public override List<string> GetLinks()
    {
        if (ContentIsStale)
        {
            GetContent();
        }

        var links = new List<string>();
        // Match Obsidian wikilinks: [[link]] or [[link|display]]
        var linkPattern = @"\[\[([^\]|]+)(?:\|[^\]]*)?\]\]";
        var matches = Regex.Matches(GetContent(), linkPattern);

        foreach (Match match in matches)
        {
            links.Add(match.Groups[1].Value);
        }

        return links;
    }

    public override List<string> GetAliases()
    {
        if (ContentIsStale)
        {
            GetContent();
        }

        var aliases = new List<string>();
        var content = GetContent();

        // Check for YAML frontmatter aliases
        var yamlPattern = @"^---\s*\n(.*?)\n---";
        var yamlMatch = Regex.Match(content, yamlPattern, RegexOptions.Singleline);

        if (yamlMatch.Success)
        {
            var frontmatter = yamlMatch.Groups[1].Value;
            var aliasPattern = @"aliases:\s*\[([^\]]+)\]";
            var aliasMatch = Regex.Match(frontmatter, aliasPattern);

            if (aliasMatch.Success)
            {
                var aliasString = aliasMatch.Groups[1].Value;
                var parts = aliasString.Split(',');
                foreach (var part in parts)
                {
                    aliases.Add(part.Trim().Trim('"', '\''));
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

        var tags = new List<string>();
        var content = GetContent();

        // Match inline tags: #tagname (but not in code blocks)
        var tagPattern = @"(?:^|\s)#([a-zA-Z0-9_-]+)";
        var matches = Regex.Matches(content, tagPattern, RegexOptions.Multiline);

        foreach (Match match in matches)
        {
            var tag = match.Groups[1].Value;
            if (!tags.Contains(tag))
            {
                tags.Add(tag);
            }
        }

        // Also check YAML frontmatter
        var yamlPattern = @"^---\s*\n(.*?)\n---";
        var yamlMatch = Regex.Match(content, yamlPattern, RegexOptions.Singleline);

        if (yamlMatch.Success)
        {
            var frontmatter = yamlMatch.Groups[1].Value;
            var tagPattern2 = @"tags:\s*\[([^\]]+)\]";
            var tagMatch = Regex.Match(frontmatter, tagPattern2);

            if (tagMatch.Success)
            {
                var tagString = tagMatch.Groups[1].Value;
                var parts = tagString.Split(',');
                foreach (var part in parts)
                {
                    var tag = part.Trim().Trim('"', '\'');
                    if (!tags.Contains(tag))
                    {
                        tags.Add(tag);
                    }
                }
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

        // Match markdown headers: # Header, ## Header, etc.
        var headerPattern = @"^(#+)\s+(.+)$";
        var matches = Regex.Matches(content, headerPattern, RegexOptions.Multiline);

        foreach (Match match in matches)
        {
            headers.Add(match.Groups[2].Value.Trim());
        }

        return headers;
    }
}
