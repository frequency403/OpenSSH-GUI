using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform;
using OpenSSHA_GUI.Views;
using OpenSSHALib.Lib;
using OpenSSHALib.Model;
using ReactiveUI;

namespace OpenSSHA_GUI.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public ReactiveCommand<Unit, Unit> OpenCreateKeyWindow => ReactiveCommand.Create(() =>
    {
        var w = new AddKeyWindow
        {
            DataContext = new AddKeyWindowViewModel(),
            ShowActivated = true,
            ShowInTaskbar = true,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        w.Show();
    });

    public Interaction<ConfirmDialogViewModel, ConfirmDialogViewModel?> ShowConfirm = new();
    
    public ReactiveCommand<SshKey, ConfirmDialogViewModel?> DeleteKey => ReactiveCommand.CreateFromTask<SshKey, ConfirmDialogViewModel?>(async (u) =>
    {

        var confirm = new ConfirmDialogViewModel("Really delete the SSH key?", "Yes", "No");
        var result = await ShowConfirm.Handle(confirm);
        if (!result.Consent) return result;
        u.DeleteKeys();
        SshKeys.Remove(u);
        return result;
    });

    
    private ObservableCollection<SshKey> _sshKeys = new (DirectoryCrawler.GetAllKeys());

    public ObservableCollection<SshKey> SshKeys
    {
        get => _sshKeys;
        set => this.RaiseAndSetIfChanged(ref _sshKeys, value);
    }
    
    
    public async Task OpenExportWindow(SshKey key)
    {
        var export = await key.ExportKey();
        if(export is null) return;
        var win = new ExportWindow
        {
            DataContext = new ExportWindowViewModel
            {
                Export = export
            },
            Title = $"Export {key.Fingerprint}",
            ShowActivated = true,
            ShowInTaskbar = true,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        win.Show();
    }
}