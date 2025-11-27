using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;

namespace WikiTools.Desktop.Services;

public class FolderPickerService : IFolderPickerService
{
    public async Task<string?> PickFolderAsync(string title)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = desktop.MainWindow;
            if (mainWindow == null)
                return null;

            var folders = await mainWindow.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = title,
                AllowMultiple = false
            });

            if (folders.Count > 0)
            {
                return folders[0].Path.LocalPath;
            }
        }

        return null;
    }
}
