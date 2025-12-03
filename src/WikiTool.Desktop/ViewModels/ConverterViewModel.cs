using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WikiTool.Converters;
using WikiTool.Desktop.Services;

namespace WikiTool.Desktop.ViewModels;

public partial class ConverterViewModel : ViewModelBase
{
    private readonly IFolderPickerService _folderPickerService;

    public ConverterViewModel(IFolderPickerService folderPickerService)
    {
        _folderPickerService = folderPickerService;
    }

    // Observable Properties
    [ObservableProperty]
    private string _sourcePath = string.Empty;

    [ObservableProperty]
    private string _destinationPath = string.Empty;

    [ObservableProperty]
    private bool _convertCategoryTags = false;

    [ObservableProperty]
    private bool _isConverting = false;

    [ObservableProperty]
    private bool _hasConversionResults = false;

    [ObservableProperty]
    private string _statusMessage = "Ready to convert";

    [ObservableProperty]
    private string _conversionLog = string.Empty;

    [ObservableProperty]
    private string? _sourcePathError;

    [ObservableProperty]
    private string? _destinationPathError;

    // Computed Property
    public bool CanConvert => !string.IsNullOrWhiteSpace(SourcePath)
                           && !string.IsNullOrWhiteSpace(DestinationPath)
                           && !IsConverting
                           && string.IsNullOrEmpty(SourcePathError)
                           && string.IsNullOrEmpty(DestinationPathError);

    // Validation Partial Methods
    partial void OnSourcePathChanged(string value)
    {
        ValidateSourcePath(value);
        ConvertCommand.NotifyCanExecuteChanged();
    }

    partial void OnDestinationPathChanged(string value)
    {
        ValidateDestinationPath(value);
        ConvertCommand.NotifyCanExecuteChanged();
    }

    private void ValidateSourcePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            SourcePathError = null;
            return;
        }

        SourcePathError = Directory.Exists(path)
            ? null
            : "Source directory does not exist";
    }

    private void ValidateDestinationPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            DestinationPathError = null;
            return;
        }

        var parentDir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir))
        {
            DestinationPathError = "Parent directory does not exist";
        }
        else
        {
            DestinationPathError = null;
        }
    }

    // Commands
    [RelayCommand]
    private async Task BrowseSourceAsync()
    {
        var path = await _folderPickerService.PickFolderAsync("Select WikidPad Source Folder");
        if (!string.IsNullOrEmpty(path))
        {
            SourcePath = path;
        }
    }

    [RelayCommand]
    private async Task BrowseDestinationAsync()
    {
        var path = await _folderPickerService.PickFolderAsync("Select Obsidian Destination Folder");
        if (!string.IsNullOrEmpty(path))
        {
            DestinationPath = path;
        }
    }

    [RelayCommand(CanExecute = nameof(CanConvert))]
    private async Task ConvertAsync()
    {
        try
        {
            IsConverting = true;
            HasConversionResults = false;
            ConversionLog = string.Empty;
            StatusMessage = "Starting conversion...";

            var logBuilder = new StringBuilder();
            logBuilder.AppendLine($"=== Conversion Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");
            logBuilder.AppendLine($"Source: {SourcePath}");
            logBuilder.AppendLine($"Destination: {DestinationPath}");
            logBuilder.AppendLine($"Convert Category Tags: {ConvertCategoryTags}");
            logBuilder.AppendLine();

            await Task.Run(() =>
            {
                var converter = new WikidPadToObsidianConverter(SourcePath, DestinationPath)
                {
                    ConvertCategoryTags = ConvertCategoryTags
                };

                logBuilder.AppendLine("Converting WikidPad files to Obsidian format...");
                converter.ConvertAll();
                logBuilder.AppendLine("Conversion completed successfully!");
            });

            logBuilder.AppendLine();
            logBuilder.AppendLine($"=== Conversion Finished: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===");

            ConversionLog = logBuilder.ToString();
            StatusMessage = "Conversion completed successfully!";
            HasConversionResults = true;
        }
        catch (DirectoryNotFoundException ex)
        {
            var logBuilder = new StringBuilder();
            logBuilder.AppendLine("ERROR: Directory not found");
            logBuilder.AppendLine($"Message: {ex.Message}");
            ConversionLog = logBuilder.ToString();
            StatusMessage = "Conversion failed - directory not found";
            HasConversionResults = true;
        }
        catch (Exception ex)
        {
            var logBuilder = new StringBuilder();
            logBuilder.AppendLine("ERROR: Conversion failed");
            logBuilder.AppendLine($"Type: {ex.GetType().Name}");
            logBuilder.AppendLine($"Message: {ex.Message}");
            logBuilder.AppendLine();
            logBuilder.AppendLine("Stack Trace:");
            logBuilder.AppendLine(ex.StackTrace);
            ConversionLog = logBuilder.ToString();
            StatusMessage = "Conversion failed - see log for details";
            HasConversionResults = true;
        }
        finally
        {
            IsConverting = false;
        }
    }

    [RelayCommand]
    private void ClearLog()
    {
        ConversionLog = string.Empty;
        HasConversionResults = false;
        StatusMessage = "Ready to convert";
    }
}
