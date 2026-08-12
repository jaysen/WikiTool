using System;
using System.Collections.Generic;

namespace WikiTool.Pages;

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

    /// <summary>
    /// Returns true if the page content contains searchStr. Matching is case-sensitive
    /// unless an alternative comparison such as OrdinalIgnoreCase is supplied.
    /// </summary>
    public abstract bool ContainsText(string searchStr, StringComparison comparison = StringComparison.Ordinal);

}