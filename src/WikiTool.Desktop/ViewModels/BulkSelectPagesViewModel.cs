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
using WikiTool.Pages;

namespace WikiTool.Desktop.ViewModels;

/// <summary>
/// Picks a source wiki and a set of its pages in bulk. Shared by every bulk operation
/// (copy, tag, convert, delete) through <see cref="BulkOperationViewModel"/>; the operation itself
/// only reads <see cref="SelectedPages"/> and never touches the tree.
/// </summary>
public partial class BulkSelectPagesViewModel : ViewModelBase
{
    private readonly ObservableCollection<WikiBrowserViewModel> _wikiTabs;

    /// <summary>Heading shown above the tree, e.g. "Select Pages to Copy".</summary>
    [ObservableProperty]
    private string _title = "Select Pages";

    [ObservableProperty]
    private WikiBrowserViewModel? _sourceWiki;

    [ObservableProperty]
    private ObservableCollection<SelectablePageNode> _selectablePages = [];

    [ObservableProperty]
    private string _searchStr = string.Empty;

    [ObservableProperty]
    private bool _matchCase;

    [ObservableProperty]
    private bool _isSearching;

    [ObservableProperty]
    private string _searchStatus = string.Empty;

    [ObservableProperty]
    private int _selectedPageCount;

    /// <summary>Set by the owning operation while it runs, so selection stays frozen until it finishes.</summary>
    [ObservableProperty]
    private bool _isOperationRunning;

    public BulkSelectPagesViewModel(
        ObservableCollection<WikiBrowserViewModel> wikiTabs,
        WikiBrowserViewModel? currentWiki = null)
    {
        _wikiTabs = wikiTabs;

        // Auto-select current wiki as source
        if (currentWiki != null && currentWiki.HasWikiLoaded)
        {
            SourceWiki = currentWiki;
        }
    }

    public IEnumerable<WikiBrowserViewModel> AvailableSourceWikis =>
        _wikiTabs.Where(w => w.HasWikiLoaded);

    public bool HasSourceWiki => SourceWiki != null;

    /// <summary>The checked pages, folders excluded.</summary>
    public IEnumerable<SelectablePageNode> SelectedPages => GetSelectedPages(SelectablePages);

    /// <summary>True while a search or an owning operation is running.</summary>
    public bool IsBusy => IsSearching || IsOperationRunning;

    partial void OnSourceWikiChanged(WikiBrowserViewModel? value)
    {
        if (value != null)
        {
            LoadSelectablePages();
        }
        else
        {
            SelectablePages.Clear();
            UpdateSelectedPageCount();
        }

        SearchStatus = string.Empty;
        OnPropertyChanged(nameof(HasSourceWiki));
    }

    partial void OnIsSearchingChanged(bool value) => OnPropertyChanged(nameof(IsBusy));

    partial void OnIsOperationRunningChanged(bool value) => OnPropertyChanged(nameof(IsBusy));

    private void LoadSelectablePages()
    {
        if (SourceWiki == null || string.IsNullOrEmpty(SourceWiki.WikiRootPath))
        {
            SelectablePages.Clear();
            UpdateSelectedPageCount();
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
    private async Task SelectBySearchStrAsync()
    {
        if (SourceWiki == null
            || string.IsNullOrWhiteSpace(SourceWiki.WikiRootPath)
            || string.IsNullOrWhiteSpace(SearchStr)
            || IsBusy)
        {
            return;
        }

        var rootPath = SourceWiki.WikiRootPath;
        var searchStr = SearchStr;
        var comparison = MatchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

        IsSearching = true;
        SearchStatus = $"Searching pages for '{searchStr}'...";

        try
        {
            // Reading every page is slow on large wikis, so keep it off the UI thread.
            var matchedPaths = await Task.Run(() =>
                WikiFactory.CreateForPath(rootPath)
                    .GetPagesBySearchStr(searchStr, comparison)
                    .OfType<LocalPage>()
                    .Select(p => p.PagePath)
                    .ToList());

            SelectPagesMatching(matchedPaths);
            SearchStatus = $"{SelectedPageCount} page(s) matched '{searchStr}'";
        }
        catch (Exception ex)
        {
            SearchStatus = $"Error searching pages: {ex.Message}";
        }
        finally
        {
            IsSearching = false;
        }
    }

    private void SelectPagesMatching(IEnumerable<string> matchedPaths)
    {
        var matchedSet = new HashSet<string>(matchedPaths, StringComparer.OrdinalIgnoreCase);
        foreach (var node in FlattenNodes(SelectablePages).Where(n => !n.IsFolder))
        {
            node.CheckState = matchedSet.Contains(node.FolderTreeNode.FullPath);
        }
    }

    private static IEnumerable<SelectablePageNode> FlattenNodes(IEnumerable<SelectablePageNode> nodes)
    {
        foreach (var node in nodes)
        {
            yield return node;
            foreach (var descendant in FlattenNodes(node.Children))
            {
                yield return descendant;
            }
        }
    }

    private static IEnumerable<SelectablePageNode> GetSelectedPages(IEnumerable<SelectablePageNode> nodes)
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
}
