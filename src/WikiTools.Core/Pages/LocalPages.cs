using System.IO;

namespace WikiTool.Core;

public abstract class LocalPage : Page
{
    public string PagePath { get; set; }

    private string _contents;
        
    protected LocalPage(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Wiki Page does not exist", path);
        }

        PagePath = path;
        Name = Path.GetFileNameWithoutExtension(path);
    }

    public override string GetContent()
    {
        _contents = File.ReadAllText(PagePath);
        ContentIsStale = true;
        return _contents;
    }

    public override bool ContainsText(string searchStr)
    {
        if (ContentIsStale)
        {
            GetContent();
        }
        return _contents.Contains(searchStr);
    }

}


public static class LocalPages
{
    public static string GetPageContent(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Wiki Page does not exist", path);
        }
        return File.ReadAllText(path);
    }

    public static bool ContainsText(string path, string searchStr)
    {
        var contents = GetPageContent(path);
        return contents.Contains(searchStr);
    }
}