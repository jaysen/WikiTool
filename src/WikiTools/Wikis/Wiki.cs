using System.Collections.Generic;

namespace WikiTools;

public abstract class Wiki
{
    public List<Page> Pages { get; set; }

    public Dictionary<string, string> Aliases { get; set; }

    /// <summary>
    /// Gets the syntax definition for this wiki format
    /// </summary>
    public abstract WikiSyntax Syntax { get; }

    public abstract List<Page> GetAllPages();
    public abstract List<Page> GetPagesBySearchStr();

}