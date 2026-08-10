using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WikiTool.Converters;

/// <summary>
/// Writes the small amount of scaffolding that turns a folder of Markdown into a
/// publishable GitHub Pages site: a _config.yml, a home page, and a tag index.
///
/// No CI workflow is generated. GitHub Pages builds Jekyll itself, and enables the
/// jekyll-relative-links plugin by default, which is what makes the plain relative
/// "page.md" links in the converted pages resolve on the published site.
/// </summary>
public class JekyllSiteWriter
{
    private readonly string _destinationPath;
    private readonly VaultIndex _index;

    /// <summary>
    /// Site title written into _config.yml and used as the home page heading.
    /// </summary>
    public string SiteTitle { get; set; } = "Wiki";

    /// <summary>
    /// Allows overwriting an existing _config.yml. Default is false, so a hand-tuned
    /// config in the destination is never silently clobbered.
    /// </summary>
    public bool Force { get; set; } = false;

    public List<string> Warnings { get; } = new List<string>();

    public JekyllSiteWriter(string destinationPath, VaultIndex index)
    {
        _destinationPath = destinationPath;
        _index = index;
    }

    public void Write()
    {
        Directory.CreateDirectory(_destinationPath);

        WriteConfig();
        WriteIndex();
        WriteTagIndex();
    }

    private void WriteConfig()
    {
        var path = Path.Combine(_destinationPath, "_config.yml");

        if (File.Exists(path) && !Force)
        {
            Warnings.Add("_config.yml already exists in the destination; left unchanged (use --force to overwrite).");
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"title: {Quote(SiteTitle)}");
        sb.AppendLine("description: \"Wiki converted from an Obsidian vault by WikiTool\"");
        sb.AppendLine("theme: jekyll-theme-primer");
        sb.AppendLine();
        sb.AppendLine("# jekyll-relative-links rewrites the relative .md links in these pages to .html");
        sb.AppendLine("# at build time, so the same files also browse correctly on github.com.");
        sb.AppendLine("plugins:");
        sb.AppendLine("  - jekyll-relative-links");
        sb.AppendLine("  - jekyll-redirect-from");
        sb.AppendLine();
        sb.AppendLine("relative_links:");
        sb.AppendLine("  enabled: true");
        sb.AppendLine("  collections: true");
        sb.AppendLine();
        sb.AppendLine("exclude:");
        sb.AppendLine("  - .obsidian");
        sb.AppendLine("  - .trash");

        File.WriteAllText(path, sb.ToString());
    }

    /// <summary>
    /// Write a home page listing every converted page, grouped by folder.
    /// Skipped if the vault already produced a page at index.md.
    /// </summary>
    private void WriteIndex()
    {
        if (HasConvertedPageAt("index.md"))
        {
            return;
        }

        var byFolder = new SortedDictionary<string, List<VaultPage>>(StringComparer.OrdinalIgnoreCase);

        foreach (var page in _index.Pages)
        {
            var folder = FolderOf(page.OutputRelPath);

            if (!byFolder.TryGetValue(folder, out var pages))
            {
                pages = new List<VaultPage>();
                byFolder[folder] = pages;
            }

            pages.Add(page);
        }

        var sb = new StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine($"title: {Quote(SiteTitle)}");
        sb.AppendLine("---");
        sb.AppendLine($"# {SiteTitle}");
        sb.AppendLine();
        sb.AppendLine($"{_index.Pages.Count} pages. See also the [tag index](tags.md).");

        foreach (var folder in byFolder)
        {
            sb.AppendLine();
            sb.AppendLine($"## {(folder.Key.Length == 0 ? "Pages" : folder.Key)}");
            sb.AppendLine();

            folder.Value.Sort((a, b) => string.Compare(a.Title, b.Title, StringComparison.OrdinalIgnoreCase));

            foreach (var page in folder.Value)
            {
                sb.AppendLine($"- [{page.Title}]({page.OutputRelPath})");
            }
        }

        File.WriteAllText(Path.Combine(_destinationPath, "index.md"), sb.ToString());
    }

    /// <summary>
    /// Write a tag index: every tag, with the pages carrying it.
    /// </summary>
    private void WriteTagIndex()
    {
        if (HasConvertedPageAt("tags.md"))
        {
            Warnings.Add("A converted page occupies tags.md; the tag index was not written.");
            return;
        }

        var byTag = new SortedDictionary<string, List<VaultPage>>(StringComparer.OrdinalIgnoreCase);

        foreach (var page in _index.Pages)
        {
            foreach (var tag in page.Tags)
            {
                if (!byTag.TryGetValue(tag, out var pages))
                {
                    pages = new List<VaultPage>();
                    byTag[tag] = pages;
                }

                pages.Add(page);
            }
        }

        var sb = new StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine("title: \"Tags\"");
        sb.AppendLine("---");
        sb.AppendLine("# Tags");

        if (byTag.Count == 0)
        {
            sb.AppendLine();
            sb.AppendLine("No tags were found in this wiki.");
        }

        foreach (var tag in byTag)
        {
            sb.AppendLine();
            sb.AppendLine($"## {tag.Key}");
            sb.AppendLine();

            tag.Value.Sort((a, b) => string.Compare(a.Title, b.Title, StringComparison.OrdinalIgnoreCase));

            foreach (var page in tag.Value)
            {
                sb.AppendLine($"- [{page.Title}]({page.OutputRelPath})");
            }
        }

        File.WriteAllText(Path.Combine(_destinationPath, "tags.md"), sb.ToString());
    }

    private bool HasConvertedPageAt(string outputRelPath)
    {
        foreach (var page in _index.Pages)
        {
            if (string.Equals(page.OutputRelPath, outputRelPath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string FolderOf(string outputRelPath)
    {
        var index = outputRelPath.LastIndexOf('/');
        return index < 0 ? string.Empty : outputRelPath.Substring(0, index);
    }

    private static string Quote(string value)
    {
        return "\"" + (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }
}
