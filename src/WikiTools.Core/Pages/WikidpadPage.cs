using System;
using System.Collections.Generic;

namespace WikiTool.Core;

public class WikidpadPage : LocalPage
{
    public WikidpadPage(string location) : base(location)
    {
        if (System.IO.Path.GetExtension(location) != ".wiki")
        {
            throw new FormatException("This is not a path to a .wiki page");
        }
    }

    public override List<string> GetLinks()
    {
        throw new System.NotImplementedException();
    }

    public override List<string> GetAliases()
    {
        throw new System.NotImplementedException();
    }

    public override List<string> GetTags()
    {
        throw new System.NotImplementedException();
    }

    public override List<string> GetHeaders()
    {
        throw new NotImplementedException();
    }
}