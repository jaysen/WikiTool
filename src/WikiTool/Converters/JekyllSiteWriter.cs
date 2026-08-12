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
    /// Name of the generated page listing, written inside <see cref="IndexFolder"/>.
    /// </summary>
    public const string PageIndexFileName = "page-index.md";

    /// <summary>
    /// Name of the generated tag listing, written inside <see cref="IndexFolder"/>.
    /// </summary>
    public const string TagIndexFileName = "tag-index.md";

    /// <summary>
    /// Site title written into _config.yml and used as the index page heading.
    /// </summary>
    public string SiteTitle { get; set; } = "Wiki";

    /// <summary>
    /// Folder the generated indexes are written into. Deliberately not the site root:
    /// index.md there becomes index.html, which belongs to the wiki's own home page.
    /// Set to an empty string to write the indexes at the root instead.
    /// </summary>
    public string IndexFolder { get; set; } = "indexes";

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
    /// Write a listing of every converted page, grouped by folder.
    /// </summary>
    private void WriteIndex()
    {
        var self = IndexPath(PageIndexFileName);

        if (HasConvertedPageAt(self))
        {
            Warnings.Add($"A converted page occupies {self}; the page index was not written.");
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
        sb.AppendLine($"title: {Quote(SiteTitle + " - All Pages")}");
        sb.AppendLine("---");
        sb.AppendLine($"# {SiteTitle} - All Pages");
        sb.AppendLine();
        sb.AppendLine($"{_index.Pages.Count} pages. See also the [tag index]({TagIndexFileName}).");

        foreach (var folder in byFolder)
        {
            sb.AppendLine();
            sb.AppendLine($"## {(folder.Key.Length == 0 ? "Pages" : folder.Key)}");
            sb.AppendLine();

            folder.Value.Sort((a, b) => string.Compare(a.Title, b.Title, StringComparison.OrdinalIgnoreCase));

            foreach (var page in folder.Value)
            {
                sb.AppendLine($"- [{page.Title}]({LinkTo(self, page)})");
            }
        }

        WriteIndexFile(self, sb.ToString());
    }

    /// <summary>
    /// Write a tag index: every tag, with the pages carrying it.
    /// </summary>
    private void WriteTagIndex()
    {
        var self = IndexPath(TagIndexFileName);

        if (HasConvertedPageAt(self))
        {
            Warnings.Add($"A converted page occupies {self}; the tag index was not written.");
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
        sb.AppendLine($"title: {Quote(SiteTitle + " - Tags")}");
        sb.AppendLine("---");
        sb.AppendLine($"# {SiteTitle} - Tags");

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
                sb.AppendLine($"- [{page.Title}]({LinkTo(self, page)})");
            }
        }

        WriteIndexFile(self, sb.ToString());
    }

    /// <summary>
    /// Output path of a generated index, honouring <see cref="IndexFolder"/>.
    /// The folder is slugged so it can never begin with '_' or '.', which Jekyll excludes.
    /// </summary>
    private string IndexPath(string fileName)
    {
        if (string.IsNullOrWhiteSpace(IndexFolder))
        {
            return fileName;
        }

        return Slug.ForPath(IndexFolder) + "/" + fileName;
    }

    /// <summary>
    /// Link from a generated index to a converted page. Indexes live in their own folder,
    /// so these have to be relative to that folder rather than to the site root.
    /// </summary>
    private static string LinkTo(string indexRelPath, VaultPage page)
    {
        return ObsidianToMarkdownConverter.MakeRelative(indexRelPath, page.OutputRelPath);
    }

    private void WriteIndexFile(string relPath, string content)
    {
        var fullPath = Path.Combine(_destinationPath, relPath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
        File.WriteAllText(fullPath, content);
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
