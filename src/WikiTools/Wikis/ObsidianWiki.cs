using System;
using System.Collections.Generic;
using System.IO;

namespace WikiTools;

public class ObsidianWiki : LocalWiki
{
    private ObsidianSyntax _syntax;

    /// <summary>
    /// Gets the Obsidian syntax definition
    /// </summary>
    public override WikiSyntax Syntax => _syntax ??= new ObsidianSyntax();

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
            pages.Add(new ObsidianPage(file, this));
        }

        return pages;
    }

    public override List<Page> GetPagesBySearchStr()
    {
        throw new NotImplementedException();
    }
}
