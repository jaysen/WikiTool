using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WikiTool.Desktop.Models;
using WikiTool.Desktop.Services;
using WikiTool.Desktop.Views;

namespace WikiTool.Desktop.ViewModels;

public partial class WikiBrowserViewModel : ViewModelBase
{
    private readonly IFolderPickerService _folderPickerService;

    public WikiBrowserViewModel(IFolderPickerService folderPickerService)
    {
        _folderPickerService = folderPickerService;
        ConverterViewModel = new ConverterViewModel(folderPickerService);
    }

    public ConverterViewModel ConverterViewModel { get; }

    [ObservableProperty]
    private string _wikiRootPath = string.Empty;

    [ObservableProperty]
    private ObservableCollection<FolderTreeNode> _folderTree = [];

    [ObservableProperty]
    private FolderTreeNode? _selectedNode;

    [ObservableProperty]
    private string _pageContent = string.Empty;

    [ObservableProperty]
    private string _selectedPageName = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "No wiki folder selected";

    [ObservableProperty]
    private string _tabTitle = "New Wiki";

    [ObservableProperty]
    private bool _isEditingTabTitle;

    [ObservableProperty]
    private bool _hasWikiLoaded;

    [ObservableProperty]
    private bool _hasPageLoaded;

    [RelayCommand]
    private void StartEditingTabTitle()
    {
        IsEditingTabTitle = true;
    }

    [RelayCommand]
    private void FinishEditingTabTitle()
    {
        IsEditingTabTitle = false;
        // Ensure tab title is not empty
        if (string.IsNullOrWhiteSpace(TabTitle))
        {
            TabTitle = string.IsNullOrEmpty(WikiRootPath) ? "New Wiki" : Path.GetFileName(WikiRootPath);
        }
    }

    partial void OnSelectedNodeChanged(FolderTreeNode? value)
    {
        if (value?.IsWikiFile == true)
        {
            LoadPageContent(value);
        }
    }

    partial void OnWikiRootPathChanged(string value)
    {
        if (!string.IsNullOrEmpty(value) && Directory.Exists(value))
        {
            ConverterViewModel.SourcePath = value;
        }
    }

    private void UpdateHasWikiLoaded()
    {
        HasWikiLoaded = !string.IsNullOrEmpty(WikiRootPath) && FolderTree.Count > 0;
    }

    public void ClearPageContent()
    {
        SelectedNode = null;
        PageContent = string.Empty;
        SelectedPageName = string.Empty;
        HasPageLoaded = false;
    }

    [RelayCommand]
    private async Task OpenConverterDialogAsync()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = desktop.MainWindow;
            if (mainWindow != null)
            {
                var converterWindow = new ConverterWindow(ConverterViewModel)
                {
                    Icon = mainWindow.Icon
                };
                await converterWindow.ShowDialog(mainWindow);
            }
        }
    }

    [RelayCommand]
    private async Task SelectWikiFolderAsync()
    {
        var path = await _folderPickerService.PickFolderAsync("Select Wiki Folder");
        if (!string.IsNullOrEmpty(path))
        {
            await LoadWikiFolderAsync(path);
        }
    }

    public async Task LoadWikiFolderAsync(string path)
    {
        if (!Directory.Exists(path))
        {
            StatusMessage = "Invalid folder path";
            return;
        }

        IsLoading = true;
        StatusMessage = "Loading wiki folder...";

        try
        {
            await Task.Run(() =>
            {
                WikiRootPath = path;
                var rootNode = BuildFolderTree(path);
                FolderTree = new ObservableCollection<FolderTreeNode>(rootNode);
            });

            UpdateHasWikiLoaded();
            var folderName = Path.GetFileName(path);
            TabTitle = string.IsNullOrEmpty(folderName) ? path : folderName;
            StatusMessage = $"Loaded: {folderName}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading folder: {ex.Message}";
            FolderTree.Clear();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private FolderTreeNode[] BuildFolderTree(string rootPath)
    {
        var rootInfo = new DirectoryInfo(rootPath);
        var nodes = new List<FolderTreeNode>();

        foreach (var directory in rootInfo.GetDirectories().OrderBy(d => d.Name))
        {
            var dirNode = CreateDirectoryNode(directory);
            nodes.Add(dirNode);
        }

        foreach (var file in rootInfo.GetFiles().Where(f => f.Extension is ".wiki" or ".md").OrderBy(f => f.Name))
        {
            var fileNode = CreateFileNode(file);
            nodes.Add(fileNode);
        }

        return [.. nodes];
    }

    private FolderTreeNode CreateDirectoryNode(DirectoryInfo dirInfo)
    {
        var node = new FolderTreeNode
        {
            Name = dirInfo.Name,
            FullPath = dirInfo.FullName,
            IsFolder = true,
            IsExpanded = false
        };

        var children = new List<FolderTreeNode>();

        foreach (var subDir in dirInfo.GetDirectories().OrderBy(d => d.Name))
        {
            var childNode = CreateDirectoryNode(subDir);
            childNode.Parent = node;
            children.Add(childNode);
        }

        foreach (var file in dirInfo.GetFiles().Where(f => f.Extension is ".wiki" or ".md").OrderBy(f => f.Name))
        {
            var fileNode = CreateFileNode(file);
            fileNode.Parent = node;
            children.Add(fileNode);
        }

        node.Children = new ObservableCollection<FolderTreeNode>(children);
        return node;
    }

    private FolderTreeNode CreateFileNode(FileInfo fileInfo)
    {
        return new FolderTreeNode
        {
            Name = fileInfo.Name,
            FullPath = fileInfo.FullName,
            IsFolder = false
        };
    }

    private void LoadPageContent(FolderTreeNode node)
    {
        SelectedPageName = node.Name;

        if (!File.Exists(node.FullPath))
        {
            PageContent = "File not found";
            return;
        }

        try
        {
            PageContent = File.ReadAllText(node.FullPath);
            HasPageLoaded = true;
        }
        catch (Exception ex)
        {
            PageContent = $"Error loading file: {ex.Message}";
        }
    }
}
