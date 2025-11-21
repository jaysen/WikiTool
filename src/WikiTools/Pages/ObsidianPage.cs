using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace WikiTools;

public class ObsidianPage : LocalPage
{
    private ObsidianWiki _wiki;

    public ObsidianPage(string location, ObsidianWiki wiki = null) : base(location)
    {
        var ext = System.IO.Path.GetExtension(location);
        if (ext != ".md")
        {
            throw new FormatException("This is not a path to a .md page");
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
        var syntax = (_wiki?.Syntax ?? new ObsidianSyntax()) as ObsidianSyntax;
        var matches = syntax.LinkPattern.Matches(content);

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

        // Use syntax patterns from parent wiki
        var syntax = (_wiki?.Syntax ?? new ObsidianSyntax()) as ObsidianSyntax;

        // Check for YAML frontmatter aliases
        var yamlMatch = ObsidianSyntax.YamlPattern.Match(content);

        if (yamlMatch.Success)
        {
            var frontmatter = yamlMatch.Groups[1].Value;
            var aliasMatch = syntax.AliasPattern.Match(frontmatter);

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

        // Use syntax patterns from parent wiki
        var syntax = (_wiki?.Syntax ?? new ObsidianSyntax()) as ObsidianSyntax;

        // Match inline tags: #tagname
        var matches = syntax.TagPattern.Matches(content);

        foreach (Match match in matches)
        {
            var tag = match.Groups[1].Value;
            if (!tags.Contains(tag))
            {
                tags.Add(tag);
            }
        }

        // Also check YAML frontmatter
        var yamlMatch = ObsidianSyntax.YamlPattern.Match(content);

        if (yamlMatch.Success)
        {
            var frontmatter = yamlMatch.Groups[1].Value;
            var tagMatch = ObsidianSyntax.YamlTagPattern.Match(frontmatter);

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

        // Use syntax pattern from parent wiki
        var syntax = (_wiki?.Syntax ?? new ObsidianSyntax()) as ObsidianSyntax;
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
        var syntax = (_wiki?.Syntax ?? new ObsidianSyntax()) as ObsidianSyntax;

        // First: Match inline attributes [key:: value]
        var inlineMatches = syntax.AttributePattern.Matches(content);

        foreach (Match match in inlineMatches)
        {
            var key = match.Groups[1].Value.Trim();
            var value = match.Groups[2].Value.Trim();
            attributes[key] = value;
        }

        // Second: Check YAML frontmatter for attributes
        var yamlMatch = ObsidianSyntax.YamlPattern.Match(content);

        if (yamlMatch.Success)
        {
            var frontmatter = yamlMatch.Groups[1].Value;

            // Match all key-value pairs in frontmatter
            var yamlAttrMatches = ObsidianSyntax.YamlAttributePattern.Matches(frontmatter);

            foreach (Match match in yamlAttrMatches)
            {
                var key = match.Groups[1].Value.Trim();
                var value = match.Groups[2].Value.Trim();

                // Skip known special keys (tags and aliases are handled separately)
                if (key == "tags" || key == "aliases")
                    continue;

                // Clean up array/list values if present
                if (value.StartsWith("[") && value.EndsWith("]"))
                {
                    value = value.Trim('[', ']').Trim();
                }

                // Remove quotes if present
                value = value.Trim('"', '\'');

                attributes[key] = value;
            }
        }

        return attributes;
    }
}
