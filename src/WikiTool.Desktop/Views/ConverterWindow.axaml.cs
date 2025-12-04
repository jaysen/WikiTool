using Avalonia.Controls;
using WikiTool.Desktop.ViewModels;

namespace WikiTool.Desktop.Views;

public partial class ConverterWindow : Window
{
    public ConverterWindow()
    {
        InitializeComponent();
    }

    public ConverterWindow(ConverterViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
}
