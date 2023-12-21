using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
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
    private string[] _wordCollection = ["Welcome", "to", "Avalonia"];

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
    
    public MainWindowViewModel()
    {
        SshKeys = new ObservableCollection<SshKey>(DirectoryCrawler.GetAllKeys());
    }

    private ObservableCollection<SshKey> _sshKeys;

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