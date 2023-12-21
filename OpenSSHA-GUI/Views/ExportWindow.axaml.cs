using Avalonia.Controls;
using Avalonia.Interactivity;
using OpenSSHA_GUI.ViewModels;

namespace OpenSSHA_GUI.Views;

public partial class ExportWindow : Window
{
    public ExportWindow()
    {
        InitializeComponent();
    }

    private async void CopyToClipboard(object? sender, RoutedEventArgs e) => await GetTopLevel(ExportedText).Clipboard.SetTextAsync((DataContext as ExportWindowViewModel).Export);
}