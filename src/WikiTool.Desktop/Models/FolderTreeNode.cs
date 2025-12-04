using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WikiTool.Desktop.Models;

/// <summary>
/// Represents a node in the folder tree structure (folder or file).
/// </summary>
public partial class FolderTreeNode : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _fullPath = string.Empty;

    [ObservableProperty]
    private bool _isFolder;

    [ObservableProperty]
    private bool _isExpanded;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private ObservableCollection<FolderTreeNode> _children = [];

    public FolderTreeNode? Parent { get; set; }

    /// <summary>
    /// Gets the file extension (empty string for folders).
    /// </summary>
    public string Extension => IsFolder ? string.Empty : Path.GetExtension(FullPath);

    /// <summary>
    /// Gets whether this node represents a wiki file (.wiki or .md).
    /// </summary>
    public bool IsWikiFile => Extension is ".wiki" or ".md";
}
