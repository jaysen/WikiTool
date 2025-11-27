using WikiTools.Desktop.Services;

namespace WikiTools.Desktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public ConverterViewModel ConverterViewModel { get; }

    public MainWindowViewModel()
    {
        var folderPickerService = new FolderPickerService();
        ConverterViewModel = new ConverterViewModel(folderPickerService);
    }
}
