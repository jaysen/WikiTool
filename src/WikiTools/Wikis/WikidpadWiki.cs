using System;
using System.Collections.Generic;
using System.IO;

namespace WikiTools;

public class WikidpadWiki : LocalWiki
{
    public readonly string DataDir;

    private WikidPadSyntax _syntax;

    /// <summary>
    /// Gets the WikidPad syntax definition
    /// </summary>
    public override WikiSyntax Syntax => _syntax ??= new WikidPadSyntax();

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
            pages.Add(new WikidpadPage(file, this));
        }
        return pages;
    }

    public override List<Page> GetPagesBySearchStr()
    {
        throw new NotImplementedException();
    }
}