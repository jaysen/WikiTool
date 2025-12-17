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
    /// Lazy-loaded index mapping page names to their corresponding Page objects.
    /// Allows efficient lookup when multiple pages may have the same name.
    /// Case-insensitive matching.
    /// </summary>
    private Dictionary<string, List<Page>> _pageNameIndex;
    public Dictionary<string, List<Page>> PageNameIndex
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
    /// Builds an index of page names to Page objects.
    /// Override this method if you need custom indexing behavior.
    /// </summary>
    protected virtual Dictionary<string, List<Page>> BuildPageNameIndex()
    {
        var index = new Dictionary<string, List<Page>>(StringComparer.OrdinalIgnoreCase);
        var pages = GetAllPages();

        foreach (var page in pages)
        {
            if (!index.ContainsKey(page.Name))
            {
                index[page.Name] = new List<Page>();
            }
            index[page.Name].Add(page);
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