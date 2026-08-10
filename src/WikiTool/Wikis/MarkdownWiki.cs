using System;
using System.Collections.Generic;
using System.IO;
using WikiTool.Pages;

namespace WikiTool.Wikis;


public class MarkdownWiki : LocalWiki
{
    private MarkdownSyntax _syntax;

    /// <summary>
    /// Gets the Markdown syntax definition
    /// </summary>
    public override WikiSyntax Syntax => _syntax ??= new MarkdownSyntax();

    public MarkdownWiki(string rootPath) : base(rootPath)
    {
        FileExtension = ".md";
    }

    public override List<Page> GetAllPages()
    {
        var files = Directory.EnumerateFiles(RootPath, $"*{FileExtension}", SearchOption.AllDirectories);
        var pages = new List<Page>();

        foreach (var file in files)
        {
            pages.Add(new MarkdownPage(file, this));
        }

        return pages;
    }

    public override List<Page> GetPagesBySearchStr()
    {
        throw new NotImplementedException();
    }
}
