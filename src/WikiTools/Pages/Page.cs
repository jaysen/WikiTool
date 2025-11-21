using System.Collections.Generic;

namespace WikiTools;

public abstract class Page
{
    public string Name { get; set; }

    public bool ContentIsStale { get; set; } = true;

    protected Page() { }
    protected Page(string name)
    {
        Name = name;
    }

    public abstract List<string> GetHeaders();
    public abstract string GetContent();

    public abstract List<string> GetLinks();
    public abstract List<string> GetAliases();
    public abstract List<string> GetTags();
    public abstract Dictionary<string, string> GetAttributes();

    public abstract bool ContainsText(string searchStr);

}