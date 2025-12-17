using System;
using System.Collections.Generic;
using WikiTool.Pages;

namespace WikiTool.Wikis;


public abstract class Wiki
{
    public List<Page> Pages { get; set; }

    public Dictionary<string, string> Aliases { get; set; }

    /// <summary>
    /// Gets the syntax definition for this wiki format
    /// </summary>
    public abstract WikiSyntax Syntax { get; }

    /// <summary>
    /// Lazy-loaded index mapping page names to their file paths (relative to wiki root).
    /// Allows efficient lookup when multiple pages may have the same name.
    /// Case-insensitive matching.
    /// </summary>
    private Dictionary<string, List<string>> _pageNameIndex;
    public Dictionary<string, List<string>> PageNameIndex
    {
        get
        {
            if (_pageNameIndex == null)
            {
                _pageNameIndex = BuildPageNameIndex();
            }
            return _pageNameIndex;
        }
    }

    /// <summary>
    /// Builds an index of page names to their relative file paths.
    /// Override this method if you need custom indexing behavior.
    /// </summary>
    protected virtual Dictionary<string, List<string>> BuildPageNameIndex()
    {
        var index = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var pages = GetAllPages();

        foreach (var page in pages)
        {
            if (!index.ContainsKey(page.Name))
            {
                index[page.Name] = new List<string>();
            }

            // Store path relative to wiki root if it's a LocalPage
            if (page is LocalPage localPage && this is LocalWiki localWiki)
            {
                var relativePath = System.IO.Path.GetRelativePath(localWiki.RootPath, localPage.PagePath);
                index[page.Name].Add(relativePath);
            }
            else
            {
                // Fallback for non-local pages
                index[page.Name].Add(page.Name);
            }
        }

        return index;
    }

    /// <summary>
    /// Clears the cached page name index. Call this if pages have been added/removed.
    /// </summary>
    public void InvalidatePageIndex()
    {
        _pageNameIndex = null;
    }

    public abstract List<Page> GetAllPages();
    public abstract List<Page> GetPagesBySearchStr();

}