using System;
using System.Collections.Generic;
using System.IO;
using WikiTool.Pages;
using WikiTool.Wikis;

namespace WikiTool.Converters;

/// <summary>
/// One page of the source vault, with the output location it will be written to.
/// </summary>
public class VaultPage
{
    /// <summary>Absolute path of the source file.</summary>
    public string SourcePath { get; set; }

    /// <summary>Vault-relative source path with '/' separators, e.g. "Projects/Alpha.md".</summary>
    public string SourceRelPath { get; set; }

    /// <summary>The original, un-slugified page name, e.g. "Alpha".</summary>
    public string Title { get; set; }

    /// <summary>Slugified output path with '/' separators, e.g. "projects/alpha.md".</summary>
    public string OutputRelPath { get; set; }

    public List<string> Aliases { get; set; } = new List<string>();

    public List<string> Tags { get; set; } = new List<string>();
}

/// <summary>
/// The first pass of an Obsidian conversion: a whole-vault index of every page.
///
/// This exists because Obsidian links resolve by page *name* against the entire vault
/// rather than by path, so no single page can be rewritten until every page is known.
/// Slugified output filenames make that doubly true - a link's destination path is only
/// knowable once the target page has been seen.
/// </summary>
public class VaultIndex
{
    private readonly Dictionary<string, VaultPage> _byPath =
        new Dictionary<string, VaultPage>(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, VaultPage> _byName =
        new Dictionary<string, VaultPage>(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, VaultPage> _byAlias =
        new Dictionary<string, VaultPage>(StringComparer.OrdinalIgnoreCase);

    public List<VaultPage> Pages { get; } = new List<VaultPage>();

    /// <summary>
    /// Non-fatal problems found while indexing, such as two pages sharing a name.
    /// </summary>
    public List<string> Warnings { get; } = new List<string>();

    /// <summary>
    /// Scan an Obsidian vault and work out where every page will land in the output.
    /// </summary>
    public static VaultIndex Build(string vaultRoot)
    {
        if (!Directory.Exists(vaultRoot))
        {
            throw new DirectoryNotFoundException($"Vault directory not found: {vaultRoot}");
        }

        var index = new VaultIndex();
        var wiki = new ObsidianWiki(vaultRoot);

        // Output paths must be unique after slugging, since "My Page" and "my-page"
        // both reduce to the same filename
        var usedOutputPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var page in wiki.GetAllPages())
        {
            var obsidianPage = (ObsidianPage)page;
            var relPath = ToRelativePath(vaultRoot, obsidianPage.PagePath);

            if (IsExcluded(relPath))
            {
                continue;
            }

            var vaultPage = new VaultPage
            {
                SourcePath = obsidianPage.PagePath,
                SourceRelPath = relPath,
                Title = obsidianPage.Name,
                Aliases = obsidianPage.GetAliases(),
                Tags = obsidianPage.GetTags()
            };

            vaultPage.OutputRelPath = index.ReserveOutputPath(relPath, usedOutputPaths);

            index.Pages.Add(vaultPage);
            index.Register(vaultPage);
        }

        return index;
    }

    /// <summary>
    /// Skip Obsidian's own metadata and any other hidden directory. These are enumerated
    /// by ObsidianWiki.GetAllPages, which recurses through everything under the root.
    /// </summary>
    private static bool IsExcluded(string relPath)
    {
        foreach (var segment in relPath.Split('/'))
        {
            if (segment.StartsWith("."))
            {
                return true;
            }
        }

        return false;
    }

    private static string ToRelativePath(string root, string fullPath)
    {
        return Path.GetRelativePath(root, fullPath).Replace('\\', '/');
    }

    /// <summary>
    /// Slugify a source path into an output path, appending a counter if the slug is taken.
    /// </summary>
    private string ReserveOutputPath(string relPath, HashSet<string> used)
    {
        var directory = Path.GetDirectoryName(relPath)?.Replace('\\', '/') ?? string.Empty;
        var name = Path.GetFileNameWithoutExtension(relPath);

        var slugDirectory = Slug.ForPath(directory);
        var slugName = Slug.ForFile(name);

        var candidate = Combine(slugDirectory, slugName + ".md");
        var suffix = 2;

        while (!used.Add(candidate))
        {
            candidate = Combine(slugDirectory, $"{slugName}-{suffix}.md");
            suffix++;
        }

        if (suffix > 2)
        {
            Warnings.Add($"Output filename collision for '{relPath}', written as '{candidate}'.");
        }

        return candidate;
    }

    private static string Combine(string directory, string fileName)
    {
        return string.IsNullOrEmpty(directory) ? fileName : $"{directory}/{fileName}";
    }

    private void Register(VaultPage page)
    {
        var pathKey = StripExtension(page.SourceRelPath);
        _byPath[pathKey] = page;

        if (_byName.TryGetValue(page.Title, out var existing))
        {
            Warnings.Add(
                $"Duplicate page name '{page.Title}': '{existing.SourceRelPath}' and '{page.SourceRelPath}'. " +
                "Ambiguous [[links]] will resolve to the first.");
        }
        else
        {
            _byName[page.Title] = page;
        }

        foreach (var alias in page.Aliases)
        {
            if (!_byAlias.ContainsKey(alias))
            {
                _byAlias[alias] = page;
            }
        }
    }

    /// <summary>
    /// Resolve an Obsidian link target, matching Obsidian's own precedence:
    /// an explicit path first, then a bare page name, then an alias.
    /// Matching is case-insensitive, as it is in Obsidian.
    /// </summary>
    public bool TryResolve(string target, out VaultPage page)
    {
        page = null;

        if (string.IsNullOrWhiteSpace(target))
        {
            return false;
        }

        var key = Normalize(target);

        return _byPath.TryGetValue(key, out page)
            || _byName.TryGetValue(key, out page)
            || _byAlias.TryGetValue(key, out page);
    }

    /// <summary>
    /// Reduce a link target to its lookup key: no leading slash, no "./", no ".md".
    /// </summary>
    private static string Normalize(string target)
    {
        var key = target.Trim().Replace('\\', '/');

        while (key.StartsWith("./"))
        {
            key = key.Substring(2);
        }

        key = key.TrimStart('/');

        return StripExtension(key);
    }

    private static string StripExtension(string path)
    {
        return path.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
            ? path.Substring(0, path.Length - 3)
            : path;
    }
}
