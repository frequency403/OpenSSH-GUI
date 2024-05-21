#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:39

#endregion

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.ReactiveUI;
using OpenSSH_GUI.ViewModels;

namespace OpenSSH_GUI.Views;

public partial class ExportWindow : ReactiveWindow<ExportWindowViewModel>
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