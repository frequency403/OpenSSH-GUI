#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:47

#endregion

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using ReactiveUI;

namespace OpenSSH_GUI.ViewModels;

public class ExportWindowViewModel : ViewModelBase<ExportWindowViewModel>
{
    public ExportWindowViewModel()
    {
        BooleanSubmit = ReactiveCommand.Create<bool, ExportWindowViewModel?>(boolean =>
        {
            if (!boolean) return null;
            if (Application.Current.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
                return null;
            var mainWindow = desktop.MainWindow;
            var clipboard = mainWindow.Clipboard;

            clipboard.SetTextAsync(Export).ConfigureAwait(false);
            return this;
        });
    }

    public string WindowTitle { get; set; } = "";
    public string Export { get; set; } = "";
}