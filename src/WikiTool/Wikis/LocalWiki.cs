using System.IO;

namespace WikiTool;

public abstract class LocalWiki : Wiki
{
    public string RootPath { get; set; }

    public string FileExtension { get; set; } 
            
    // Constructor
    protected LocalWiki(string rootPath)
    {
        if (!Directory.Exists(rootPath))
        {
            throw new DirectoryNotFoundException($"Directory {rootPath} does not exist");
        }
        RootPath = rootPath;
    }
}