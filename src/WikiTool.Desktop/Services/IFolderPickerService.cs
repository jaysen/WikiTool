namespace WikiTool.Desktop.Services;

public interface IFolderPickerService
{
    System.Threading.Tasks.Task<string?> PickFolderAsync(string title);
}
