using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WikiTool.Desktop.Models;
using WikiTool.Desktop.Services;

namespace WikiTool.Desktop.ViewModels;

public partial class CopyPagesViewModel : ViewModelBase
{
    private readonly ObservableCollection<WikiBrowserViewModel> _wikiTabs;
    private readonly IFolderPickerService _folderPickerService;
    private readonly Action<string>? _openFolderAsTabCallback;

    [ObservableProperty]
    private WikiBrowserViewModel? _sourceWiki;

    [ObservableProperty]
    private string? _destinationFolder;

    [ObservableProperty]
    private ObservableCollection<SelectablePageNode> _selectablePages = [];

    [ObservableProperty]
    private bool _isProcessing;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private int _selectedPageCount;

    [ObservableProperty]
    private bool _preserveFolderStructure = true;

    [ObservableProperty]
    private bool _overwriteExisting;

    [ObservableProperty]
    private bool _openDestinationAsTab;

    public CopyPagesViewModel(
        ObservableCollection<WikiBrowserViewModel> wikiTabs,
        IFolderPickerService folderPickerService,
        WikiBrowserViewModel? currentWiki = null,
        Action<string>? openFolderAsTabCallback = null)
    {
        _wikiTabs = wikiTabs;
        _folderPickerService = folderPickerService;
        _openFolderAsTabCallback = openFolderAsTabCallback;

        // Auto-select current wiki as source
        if (currentWiki != null && currentWiki.HasWikiLoaded)
        {
            SourceWiki = currentWiki;
        }
    }

    public IEnumerable<WikiBrowserViewModel> AvailableSourceWikis =>
        _wikiTabs.Where(w => w.HasWikiLoaded);

    public bool HasDestinationFolder => !string.IsNullOrEmpty(DestinationFolder);

    public string DestinationFolderDisplay =>
        string.IsNullOrEmpty(DestinationFolder) ? "No folder selected" : DestinationFolder;

    partial void OnSourceWikiChanged(WikiBrowserViewModel? value)
    {
        if (value != null)
        {
            LoadSelectablePages();
        }
        else
        {
            SelectablePages.Clear();
        }
        UpdateStatusMessage();
    }

    partial void OnDestinationFolderChanged(string? value)
    {
        OnPropertyChanged(nameof(HasDestinationFolder));
        OnPropertyChanged(nameof(DestinationFolderDisplay));
        UpdateStatusMessage();
    }

    private void LoadSelectablePages()
    {
        if (SourceWiki == null || string.IsNullOrEmpty(SourceWiki.WikiRootPath))
        {
            SelectablePages.Clear();
            return;
        }

        var nodes = new List<SelectablePageNode>();
        BuildSelectableTree(SourceWiki.FolderTree, nodes);
        SelectablePages = new ObservableCollection<SelectablePageNode>(nodes);
        UpdateSelectedPageCount();
    }

    private void BuildSelectableTree(IEnumerable<FolderTreeNode> folderNodes, ICollection<SelectablePageNode> result, string relativePath = "", SelectablePageNode? parent = null)
    {
        foreach (var node in folderNodes)
        {
            var selectableNode = new SelectablePageNode(node, relativePath, parent);
            selectableNode.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SelectablePageNode.CheckState))
                {
                    UpdateSelectedPageCount();
                }
            };

            if (node.IsFolder && node.Children.Count > 0)
            {
                var newRelativePath = string.IsNullOrEmpty(relativePath)
                    ? node.Name
                    : Path.Combine(relativePath, node.Name);
                BuildSelectableTree(node.Children, selectableNode.Children, newRelativePath, selectableNode);
            }

            result.Add(selectableNode);
        }
    }

    private void UpdateSelectedPageCount()
    {
        SelectedPageCount = CountSelectedPages(SelectablePages);
        UpdateStatusMessage();
    }

    private int CountSelectedPages(IEnumerable<SelectablePageNode> nodes)
    {
        int count = 0;
        foreach (var node in nodes)
        {
            if (node.CheckState == true && !node.IsFolder)
            {
                count++;
            }
            count += CountSelectedPages(node.Children);
        }
        return count;
    }

    private void UpdateStatusMessage()
    {
        if (SourceWiki == null)
        {
            StatusMessage = "Select a source wiki";
        }
        else if (string.IsNullOrEmpty(DestinationFolder))
        {
            StatusMessage = $"{SelectedPageCount} pages selected. Select a destination folder.";
        }
        else
        {
            StatusMessage = $"Ready to copy {SelectedPageCount} pages to '{Path.GetFileName(DestinationFolder)}'";
        }
    }

    [RelayCommand]
    private async Task BrowseDestinationFolderAsync()
    {
        var folder = await _folderPickerService.PickFolderAsync("Select Destination Folder");
        if (!string.IsNullOrEmpty(folder))
        {
            DestinationFolder = folder;
        }
    }

    [RelayCommand]
    private void SelectAllPages()
    {
        SetAllPagesSelected(SelectablePages, true);
        UpdateSelectedPageCount();
    }

    [RelayCommand]
    private void DeselectAllPages()
    {
        SetAllPagesSelected(SelectablePages, false);
        UpdateSelectedPageCount();
    }

    private void SetAllPagesSelected(IEnumerable<SelectablePageNode> nodes, bool isSelected)
    {
        foreach (var node in nodes)
        {
            node.CheckState = isSelected;
        }
    }

    [RelayCommand]
    private async Task CopyPagesAsync()
    {
        if (SourceWiki == null || string.IsNullOrEmpty(DestinationFolder) || SelectedPageCount == 0)
        {
            return;
        }

        IsProcessing = true;
        StatusMessage = "Copying pages...";

        try
        {
            var selectedPages = GetSelectedPages(SelectablePages).ToList();
            int copiedCount = 0;

            foreach (var page in selectedPages)
            {
                await CopyPageAsync(page);
                copiedCount++;
                StatusMessage = $"Copied {copiedCount}/{selectedPages.Count} pages...";
            }

            StatusMessage = $"Successfully copied {copiedCount} pages!";

            // Open destination folder as a new tab if requested
            if (OpenDestinationAsTab && _openFolderAsTabCallback != null)
            {
                _openFolderAsTabCallback(DestinationFolder);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error copying pages: {ex.Message}";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    private IEnumerable<SelectablePageNode> GetSelectedPages(IEnumerable<SelectablePageNode> nodes)
    {
        foreach (var node in nodes)
        {
            if (node.CheckState == true && !node.IsFolder)
            {
                yield return node;
            }

            foreach (var child in GetSelectedPages(node.Children))
            {
                yield return child;
            }
        }
    }

    private async Task CopyPageAsync(SelectablePageNode page)
    {
        if (SourceWiki == null || string.IsNullOrEmpty(DestinationFolder))
        {
            return;
        }

        var sourceFile = page.FolderTreeNode.FullPath;
        string targetFile;

        if (PreserveFolderStructure)
        {
            // Preserve folder structure
            var targetFolder = Path.Combine(DestinationFolder, page.RelativePath);
            Directory.CreateDirectory(targetFolder);
            targetFile = Path.Combine(targetFolder, page.FolderTreeNode.Name);
        }
        else
        {
            // Copy to root of destination folder
            targetFile = Path.Combine(DestinationFolder, page.FolderTreeNode.Name);
        }

        if (File.Exists(targetFile) && !OverwriteExisting)
        {
            // Skip if file exists and overwrite is disabled
            return;
        }

        await Task.Run(() => File.Copy(sourceFile, targetFile, OverwriteExisting));
    }
}

public partial class SelectablePageNode : ObservableObject
{
    private bool _isUpdatingSelection;

    public FolderTreeNode FolderTreeNode { get; }
    public string RelativePath { get; }
    public SelectablePageNode? Parent { get; set; }

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool? _checkState; // true = checked, false = unchecked, null = indeterminate

    public ObservableCollection<SelectablePageNode> Children { get; } = [];

    public string Name => FolderTreeNode.Name;
    public bool IsFolder => FolderTreeNode.IsFolder;

    public SelectablePageNode(FolderTreeNode node, string relativePath, SelectablePageNode? parent = null)
    {
        FolderTreeNode = node;
        RelativePath = relativePath;
        Parent = parent;
        _checkState = false;
    }

    partial void OnCheckStateChanged(bool? value)
    {
        if (_isUpdatingSelection) return;

        _isUpdatingSelection = true;
        try
        {
            // When check state changes from user interaction (not null/indeterminate click)
            if (value.HasValue)
            {
                IsSelected = value.Value;

                // Propagate to all children
                SetChildrenCheckState(value.Value);
            }

            // Update parent state
            Parent?.UpdateCheckStateFromChildren();
        }
        finally
        {
            _isUpdatingSelection = false;
        }
    }

    private void SetChildrenCheckState(bool isChecked)
    {
        foreach (var child in Children)
        {
            child._isUpdatingSelection = true;
            child.CheckState = isChecked;
            child.IsSelected = isChecked;
            child.SetChildrenCheckState(isChecked);
            child._isUpdatingSelection = false;
        }
    }

    public void UpdateCheckStateFromChildren()
    {
        if (_isUpdatingSelection) return;
        if (Children.Count == 0) return;

        _isUpdatingSelection = true;
        try
        {
            var allChildren = GetAllDescendants(this).ToList();
            var checkedCount = allChildren.Count(c => c.CheckState == true);
            var totalCount = allChildren.Count;

            if (checkedCount == 0)
            {
                CheckState = false;
                IsSelected = false;
            }
            else if (checkedCount == totalCount)
            {
                CheckState = true;
                IsSelected = true;
            }
            else
            {
                CheckState = null; // Indeterminate
                IsSelected = false;
            }

            // Continue updating up the tree
            Parent?.UpdateCheckStateFromChildren();
        }
        finally
        {
            _isUpdatingSelection = false;
        }
    }

    private static IEnumerable<SelectablePageNode> GetAllDescendants(SelectablePageNode node)
    {
        foreach (var child in node.Children)
        {
            yield return child;
            foreach (var descendant in GetAllDescendants(child))
            {
                yield return descendant;
            }
        }
    }
}
