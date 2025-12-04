using WikiTool.Desktop.Services;

namespace WikiTool.Desktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public ConverterViewModel ConverterViewModel { get; }
    public WikiBrowserViewModel WikiBrowserViewModel { get; }

    public MainWindowViewModel()
    {
        var folderPickerService = new FolderPickerService();
        ConverterViewModel = new ConverterViewModel(folderPickerService);
        WikiBrowserViewModel = new WikiBrowserViewModel(folderPickerService);
    }
}
