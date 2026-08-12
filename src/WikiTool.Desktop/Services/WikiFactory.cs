using System.IO;
using System.Linq;
using WikiTool.Wikis;

namespace WikiTool.Desktop.Services;

/// <summary>
/// Builds a Core Wiki instance for a folder browsed in the GUI. The browser tree accepts both
/// .wiki and .md files without recording which format the folder is, so this detects the format
/// from the files actually present (WikidPad's "data" subfolder / .wiki files take precedence).
/// </summary>
public static class WikiFactory
{
    public static Wiki CreateForPath(string rootPath)
    {
        var dataDir = Path.Combine(rootPath, "data");
        var wikiFilesDir = Directory.Exists(dataDir) ? dataDir : rootPath;

        var hasWikidPadFiles = Directory.Exists(wikiFilesDir)
            && Directory.EnumerateFiles(wikiFilesDir, "*.wiki").Any();

        if (hasWikidPadFiles)
        {
            return new WikidpadWiki(rootPath);
        }

        return new MarkdownWiki(rootPath);
    }
}
