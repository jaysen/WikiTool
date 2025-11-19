using System;
using System.Collections.Generic;
using System.IO;

namespace WikiTools;

public class ObsidianWiki : LocalWiki
{
    public ObsidianWiki(string rootPath) : base(rootPath)
    {
        FileExtension = ".md";
    }

    public override List<Page> GetAllPages()
    {
        var files = Directory.EnumerateFiles(RootPath, $"*{FileExtension}", SearchOption.AllDirectories);
        var pages = new List<Page>();

        foreach (var file in files)
        {
            pages.Add(new ObsidianPage(file));
        }

        return pages;
    }

    public override List<Page> GetPagesBySearchStr()
    {
        throw new NotImplementedException();
    }
}
