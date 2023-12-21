using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using OpenSSHA_GUI.ViewModels;
using OpenSSHALib.Model;

namespace OpenSSHA_GUI.Views;

public partial class ExportWindow : Window
{
    public ExportWindow()
    {
        InitializeComponent();
    }

    private async void CopyToClipboard(object? sender, RoutedEventArgs e)
    {
        var dc = DataContext as ExportWindowViewModel;
        var toplevel = TopLevel.GetTopLevel(ExportedText);
        await toplevel.Clipboard.SetTextAsync(dc.Export);
    }
}