using System.Collections.Generic;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using WikiTool.Desktop.Models;

namespace WikiTool.Desktop.ViewModels;

/// <summary>
/// Base for operations that act on a bulk selection of pages (copy, tag, convert, delete).
/// Owns the shared <see cref="BulkSelectPagesViewModel"/> and the progress/status plumbing so a
/// derived operation only adds its own options and its action command.
/// </summary>
public abstract partial class BulkOperationViewModel : ViewModelBase
{
    protected BulkOperationViewModel(BulkSelectPagesViewModel pageSelection)
    {
        PageSelection = pageSelection;
        PageSelection.PropertyChanged += OnPageSelectionPropertyChanged;
    }

    /// <summary>The shared page picker, bound by <c>BulkSelectPagesView</c>.</summary>
    public BulkSelectPagesViewModel PageSelection { get; }

    [ObservableProperty]
    private bool _isProcessing;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public int SelectedPageCount => PageSelection.SelectedPageCount;

    protected IEnumerable<SelectablePageNode> SelectedPages => PageSelection.SelectedPages;

    partial void OnIsProcessingChanged(bool value) => PageSelection.IsOperationRunning = value;

    private void OnPageSelectionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(BulkSelectPagesViewModel.SelectedPageCount):
                OnPropertyChanged(nameof(SelectedPageCount));
                UpdateStatusMessage();
                break;
            case nameof(BulkSelectPagesViewModel.SourceWiki):
                UpdateStatusMessage();
                break;
        }
    }

    /// <summary>
    /// Refreshes <see cref="StatusMessage"/> for the current selection and operation options.
    /// Called whenever the selection changes; call it from the derived constructor to set the
    /// initial message.
    /// </summary>
    protected abstract void UpdateStatusMessage();
}
