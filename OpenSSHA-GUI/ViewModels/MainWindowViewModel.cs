using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
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
    
    public MainWindowViewModel()
    {
        Greeting = "Welcome to Avalonia!";
        SshKeys = new ObservableCollection<SSHKey>(DirectoryCrawler.GetAllKeys());
    }

    private ObservableCollection<SSHKey> _sshKeys;

    public ObservableCollection<SSHKey> SshKeys
    {
        get => _sshKeys;
        set => this.RaiseAndSetIfChanged(ref _sshKeys, value);
    }
    
    private string _greeting;
    public string Greeting
    {
        get => _greeting;
        set => this.RaiseAndSetIfChanged(ref _greeting, value);
    }  

    public void Shuffle()
    {
        var random = new Random();
        random.Shuffle(_wordCollection);
        Greeting = $"{_wordCollection[0]} {_wordCollection[1]} {_wordCollection[2]}";
        Console.WriteLine($"Greeting is: {Greeting}");

        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
    }

    public async Task OpenExportWindow(SSHKey key)
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