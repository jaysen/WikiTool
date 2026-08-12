using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WikiTool.Desktop.Models;
using WikiTool.Desktop.Services;

namespace WikiTool.Desktop.ViewModels;

public partial class CopyPagesViewModel : BulkOperationViewModel
{
    private readonly IFolderPickerService _folderPickerService;
    private readonly Action<string>? _openFolderAsTabCallback;

    [ObservableProperty]
    private string? _destinationFolder;

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
        : base(new BulkSelectPagesViewModel(wikiTabs, currentWiki) { Title = "Select Pages to Copy" })
    {
        _folderPickerService = folderPickerService;
        _openFolderAsTabCallback = openFolderAsTabCallback;

        UpdateStatusMessage();
    }

    public bool HasDestinationFolder => !string.IsNullOrEmpty(DestinationFolder);

    public string DestinationFolderDisplay =>
        string.IsNullOrEmpty(DestinationFolder) ? "No folder selected" : DestinationFolder;

    partial void OnDestinationFolderChanged(string? value)
    {
        OnPropertyChanged(nameof(HasDestinationFolder));
        OnPropertyChanged(nameof(DestinationFolderDisplay));
        UpdateStatusMessage();
    }

    protected override void UpdateStatusMessage()
    {
        if (PageSelection.SourceWiki == null)
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
    private async Task CopyPagesAsync()
    {
        if (PageSelection.SourceWiki == null || string.IsNullOrEmpty(DestinationFolder) || SelectedPageCount == 0)
        {
            return;
        }

        IsProcessing = true;
        StatusMessage = "Copying pages...";

        try
        {
            var selectedPages = SelectedPages.ToList();
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

    private async Task CopyPageAsync(SelectablePageNode page)
    {
        if (string.IsNullOrEmpty(DestinationFolder))
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
