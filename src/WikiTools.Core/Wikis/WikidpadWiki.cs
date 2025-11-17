using System;
using System.Collections.Generic;
using System.IO;

namespace WikiTool.Core;

public class WikidpadWiki : LocalWiki
{
    public readonly string DataDir;
    public WikidpadWiki(string rootPath) : base(rootPath)
    {
        DataDir = Path.Combine(rootPath, "data");
        if (!Directory.Exists(DataDir))
        {
            DataDir = rootPath;
        }

        FileExtension = ".wiki";
    }

    public override List<Page> GetAllPages()
    {
        var files = Directory.EnumerateFiles(DataDir, $"*{FileExtension}");
        var pages = new List<Page>();
        foreach (var file in files)
        {
            pages.Add(new WikidpadPage(Path.Combine(DataDir, file)));
        }
        return pages;
    }

    public override List<Page> GetPagesBySearchStr()
    {
        throw new NotImplementedException();
    }
}