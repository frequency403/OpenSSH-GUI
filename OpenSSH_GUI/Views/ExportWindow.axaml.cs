#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 04.01.2024 - 09:01:54
// Last edit: 14.05.2024 - 03:05:35

#endregion

using Avalonia.Controls;
using Avalonia.Interactivity;
using OpenSSH_GUI.ViewModels;

namespace OpenSSH_GUI.Views;

public partial class ExportWindow : Window
{
    public ExportWindow()
    {
        InitializeComponent();
    }

    private async void CopyToClipboard(object? sender, RoutedEventArgs e)
    {
        await GetTopLevel(ExportedText)!.Clipboard!.SetTextAsync((DataContext as ExportWindowViewModel)!.Export);
    }
}