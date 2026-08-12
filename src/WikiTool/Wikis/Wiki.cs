using System.Collections.Generic;
using System.Linq;
using WikiTool.Pages;

namespace WikiTool.Wikis;


public abstract class Wiki
{
    public List<Page> Pages { get; set; }

    public List<Page> SelectedPages { get; set; } = new();

    public Dictionary<string, string> Aliases { get; set; }

    /// <summary>
    /// Gets the syntax definition for this wiki format
    /// </summary>
    public abstract WikiSyntax Syntax { get; }

    public abstract List<Page> GetAllPages();

    /// <summary>
    /// Populates and returns SelectedPages with all pages whose content contains searchStr
    /// </summary>
    public virtual List<Page> GetPagesBySearchStr(string searchStr)
    {
        SelectedPages = GetAllPages().Where(page => page.ContainsText(searchStr)).ToList();
        return SelectedPages;
    }

}