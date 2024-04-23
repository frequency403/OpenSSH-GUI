using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Material.Icons;
using Material.Icons.Avalonia;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using OpenSSHALib.Lib;
using OpenSSHALib.Models;
using ReactiveUI;

namespace OpenSSHA_GUI.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public readonly Interaction<ConnectToServerViewModel, ConnectToServerViewModel?> ShowConnectToServerWindow = new();
    public readonly Interaction<AddKeyWindowViewModel, AddKeyWindowViewModel?> ShowCreate = new();

    public readonly Interaction<EditAuthorizedKeysViewModel, EditAuthorizedKeysViewModel?> ShowEditAuthorizedKeys =
        new();

    public readonly Interaction<EditKnownHostsViewModel, EditKnownHostsViewModel?> ShowEditKnownHosts = new();
    public readonly Interaction<ExportWindowViewModel, ExportWindowViewModel?> ShowExportWindow = new();

    private MaterialIcon _itemsCount = new()
    {
        Kind = MaterialIconKind.NumericZero,
        Width = 20,
        Height = 20
    };

    private ServerConnection _serverConnection;

    private ObservableCollection<SshPublicKey> _sshKeys;

    public MainWindowViewModel()
    {
        RxApp.MainThreadScheduler.Schedule(Initialization);
        _sshKeys = new ObservableCollection<SshPublicKey>(DirectoryCrawler.GetAllKeys(out var errors));

        foreach (var error in errors)
        {
            Console.WriteLine($"Problem loading \"{error.File}\": {error.Exception.Message}");
        }
        
        _serverConnection = new ServerConnection("123", "123", "123");
        EvaluateAppropriateIcon();
    }

    public MaterialIcon ItemsCount
    {
        get => _itemsCount;
        set => this.RaiseAndSetIfChanged(ref _itemsCount, value);
    }

    public ReactiveCommand<Unit, Unit> NotImplementedMessage => ReactiveCommand.CreateFromTask<Unit, Unit>(async e =>
    {
        var msgBox = MessageBoxManager.GetMessageBoxStandard("Not Implemented yet",
            "This function is not implemented yet, but planned!", ButtonEnum.Ok, Icon.Info);
        await msgBox.ShowAsync();
        return e;
    });

    public ReactiveCommand<string, Unit?> OpenBrowser => ReactiveCommand.Create<string, Unit?>(e =>
    {
        var url = int.Parse(e) switch
        {
            1 => "https://github.com/frequency403/OpenSSH-GUI/issues",
            2 => "https://github.com/frequency403/OpenSSH-GUI#authors",
            _ => "https://github.com/frequency403/OpenSSH-GUI"
        };

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            url = url.Replace("&", "^&");
            Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Process.Start("xdg-open", url);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Process.Start("open", url);
        }

        return null;
    });

    public ReactiveCommand<Unit, Unit> DisconnectServer => ReactiveCommand.CreateFromTask<Unit, Unit>(async e =>
    {
        var messageBoxText = "Disconnected successfully!";
        var messageBoxIcon = Icon.Success;
        if (ServerConnection.IsConnected)
        {
            if (!ServerConnection.CloseConnection(out var exception))
            {
                messageBoxText = exception.Message;
                messageBoxIcon = Icon.Error;
            }
        }
        else
        {
            messageBoxText = "Nothing to disconnect from!";
            messageBoxIcon = Icon.Error;
        }

        var msgBox = MessageBoxManager.GetMessageBoxStandard("Disconnect from Server", messageBoxText,
            ButtonEnum.Ok, messageBoxIcon);
        await msgBox.ShowAsync();
        return e;
    });

    public ReactiveCommand<Unit, ConnectToServerViewModel?> OpenConnectToServerWindow =>
        ReactiveCommand.CreateFromTask<Unit, ConnectToServerViewModel?>(async e =>
        {
            var connectToServer = new ConnectToServerViewModel(ref _sshKeys);
            var windowResult = await ShowConnectToServerWindow.Handle(connectToServer);
            if (windowResult is not null) ServerConnection = windowResult.ServerConnection;
            return windowResult;
        });

    public ReactiveCommand<Unit, EditKnownHostsViewModel?> OpenEditKnownHostsWindow =>
        ReactiveCommand.CreateFromTask<Unit, EditKnownHostsViewModel?>(async e =>
        {
            var editKnownHosts = new EditKnownHostsViewModel(ref _serverConnection);
            return await ShowEditKnownHosts.Handle(editKnownHosts);
        });

    public ReactiveCommand<SshKey, ExportWindowViewModel?> OpenExportKeyWindow =>
        ReactiveCommand.CreateFromTask<SshKey, ExportWindowViewModel?>(async key =>
        {
            var keyExport = await key.ExportKeyAsync();
            if (keyExport is null)
            {
                var alert = MessageBoxManager.GetMessageBoxStandard(StringsAndTexts.Error,
                    StringsAndTexts.MainWindowViewModelExportKeyErrorMessage,
                    ButtonEnum.Ok, Icon.Error);
                await alert.ShowAsync();
                return null;
            }

            var exportViewModel = new ExportWindowViewModel
            {
                WindowTitle = string.Format(StringsAndTexts.MainWindowViewModelDynamicExportWindowTitle,
                    key.KeyTypeString, key.Fingerprint),
                Export = keyExport
            };
            return await ShowExportWindow.Handle(exportViewModel);
        });

    public ReactiveCommand<Unit, EditAuthorizedKeysViewModel?> OpenEditAuthorizedKeysWindow =>
        ReactiveCommand.CreateFromTask<Unit, EditAuthorizedKeysViewModel?>(
            async e =>
            {
                try
                {
                    var editAuthorizedKeysViewModel =
                        new EditAuthorizedKeysViewModel(ref _serverConnection, ref _sshKeys);
                    return await ShowEditAuthorizedKeys.Handle(editAuthorizedKeysViewModel);
                }
                catch (Exception exception)
                {
                    var messageBox = MessageBoxManager.GetMessageBoxStandard(StringsAndTexts.Error, exception.Message,
                        ButtonEnum.Ok, Icon.Error);
                    await messageBox.ShowAsync();
                    return null;
                }
            });

    public ReactiveCommand<Unit, AddKeyWindowViewModel?> OpenCreateKeyWindow =>
        ReactiveCommand.CreateFromTask<Unit, AddKeyWindowViewModel?>(async e =>
        {
            var create = new AddKeyWindowViewModel();
            var result = await ShowCreate.Handle(create);
            if (result == null) return result;
            Exception? exception = null;
            await Task.Run(async () =>
            {
                try
                {
                    var newKey = await result.RunKeyGen();
                    if (newKey != null) SshKeys.Add(newKey);
                }
                catch (Exception e1)
                {
                    exception = e1;
                }
            });
            if (exception is null) return result;
            var msgBox = MessageBoxManager.GetMessageBoxStandard(StringsAndTexts.Error, exception.Message,
                ButtonEnum.Ok, Icon.Error);
            await msgBox.ShowAsync();
            EvaluateAppropriateIcon();
            return result;
        });

    public ReactiveCommand<SshPublicKey, SshPublicKey?> DeleteKey =>
        ReactiveCommand.CreateFromTask<SshPublicKey, SshPublicKey?>(async u =>
        {
            var box = MessageBoxManager.GetMessageBoxStandard(
                string.Format(StringsAndTexts.MainWindowViewModelDeleteKeyTitleText, u.Filename, u.PrivateKey.Filename),
                StringsAndTexts.MainWindowViewModelDeleteKeyQuestionText, ButtonEnum.YesNo, Icon.Question);
            var res = await box.ShowAsync();
            if (res != ButtonResult.Yes) return null;
            u.DeleteKey();
            SshKeys.Remove(u);
            EvaluateAppropriateIcon();
            return u;
        });

    public ServerConnection ServerConnection
    {
        get => _serverConnection;
        private set => this.RaiseAndSetIfChanged(ref _serverConnection, value);
    }

    public ObservableCollection<SshPublicKey> SshKeys
    {
        get => _sshKeys;
        set => this.RaiseAndSetIfChanged(ref _sshKeys, value);
    }

    private static void Initialization()
    {
        InitializationRoutine.MakeProgramStartReady();
    }

    private void EvaluateAppropriateIcon()
    {
        ItemsCount = new MaterialIcon
        {
            Kind = SshKeys.Count switch
            {
                1 => MaterialIconKind.NumericOne,
                2 => MaterialIconKind.NumericTwo,
                3 => MaterialIconKind.NumericThree,
                4 => MaterialIconKind.NumericFour,
                5 => MaterialIconKind.NumericFive,
                6 => MaterialIconKind.NumericSix,
                7 => MaterialIconKind.NumericSeven,
                8 => MaterialIconKind.NumericEight,
                9 => MaterialIconKind.NumericNine,
                10 => MaterialIconKind.Numeric10,
                _ => MaterialIconKind.Infinity
            },
            Width = 20,
            Height = 20
        };
    }
}