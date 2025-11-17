using System.Collections.Generic;

namespace WikiTool.Core;

public abstract class Wiki
{
    public List<Page> Pages { get; set; }

    public Dictionary<string, string> Aliases { get; set; }

    public abstract List<Page> GetAllPages();
    public abstract List<Page> GetPagesBySearchStr();
        
}