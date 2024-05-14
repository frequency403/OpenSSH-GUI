#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 14.05.2024 - 00:05:30
// Last edit: 14.05.2024 - 03:05:26

#endregion

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Material.Icons;
using Material.Icons.Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using OpenSSH_GUI.Assets;
using OpenSSH_GUI.Core.Interfaces.Keys;
using OpenSSH_GUI.Core.Interfaces.Misc;
using OpenSSH_GUI.Core.Interfaces.Settings;
using OpenSSH_GUI.Core.Lib;
using OpenSSH_GUI.Core.Lib.Misc;
using ReactiveUI;
using SshNet.Keygen;

namespace OpenSSH_GUI.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public readonly Interaction<ApplicationSettingsViewModel, ApplicationSettingsViewModel?> ShowAppSettings = new();
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

    private IServerConnection _serverConnection;

    private ObservableCollection<ISshKey?> _sshKeys;

    public MainWindowViewModel(ILogger<MainWindowViewModel> logger) : base(logger)
    {
        _sshKeys = new ObservableCollection<ISshKey?>(App.ServiceProvider.GetRequiredService<IApplicationSettings>().Crawler.GetAllKeys());
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
            var connectToServer = App.ServiceProvider.GetRequiredService<ConnectToServerViewModel>();
            connectToServer.SetKeys(ref _sshKeys);
            var windowResult = await ShowConnectToServerWindow.Handle(connectToServer);
            if (windowResult is not null) ServerConnection = windowResult.ServerConnection;
            return windowResult;
        });

    public ReactiveCommand<Unit, EditKnownHostsViewModel?> OpenEditKnownHostsWindow =>
        ReactiveCommand.CreateFromTask<Unit, EditKnownHostsViewModel?>(async e =>
        {
            var editKnownHosts = App.ServiceProvider.GetRequiredService<EditKnownHostsViewModel>();
            editKnownHosts.SetServerConnection(ref _serverConnection);
            return await ShowEditKnownHosts.Handle(editKnownHosts);
        });

    public ReactiveCommand<ISshKey, ExportWindowViewModel?> OpenExportKeyWindowPublic =>
        ReactiveCommand.CreateFromTask<ISshKey, ExportWindowViewModel?>(async key =>
        {
            var keyExport = key is not IPpkKey ? key.ExportTextOfKey() : key.ExportOpenSshPublicKey();
            if (keyExport is null)
            {
                var alert = MessageBoxManager.GetMessageBoxStandard(StringsAndTexts.Error,
                    StringsAndTexts.MainWindowViewModelExportKeyErrorMessage,
                    ButtonEnum.Ok, Icon.Error);
                await alert.ShowAsync();
                return null;
            }

            var exportViewModel = App.ServiceProvider.GetRequiredService<ExportWindowViewModel>();
            exportViewModel.Export = keyExport;
            exportViewModel.WindowTitle = string.Format(StringsAndTexts.MainWindowViewModelDynamicExportWindowTitle,
                key.KeyTypeString, key.Fingerprint);
            return await ShowExportWindow.Handle(exportViewModel);
        });

    public ReactiveCommand<ISshKey, ExportWindowViewModel?> OpenExportKeyWindowPrivate =>
        ReactiveCommand.CreateFromTask<ISshKey, ExportWindowViewModel?>(async key =>
        {
            var keyExport = key is not IPpkKey ? key.ExportTextOfKey() : key.ExportOpenSshPrivateKey();
            if (keyExport is null)
            {
                var alert = MessageBoxManager.GetMessageBoxStandard(StringsAndTexts.Error,
                    StringsAndTexts.MainWindowViewModelExportKeyErrorMessage,
                    ButtonEnum.Ok, Icon.Error);
                await alert.ShowAsync();
                return null;
            }

            var exportViewModel = App.ServiceProvider.GetRequiredService<ExportWindowViewModel>();
            exportViewModel.Export = keyExport;
            exportViewModel.WindowTitle = string.Format(StringsAndTexts.MainWindowViewModelDynamicExportWindowTitle,
                key.KeyTypeString, key.Fingerprint);
            return await ShowExportWindow.Handle(exportViewModel);
        });

    public ReactiveCommand<Unit, EditAuthorizedKeysViewModel?> OpenEditAuthorizedKeysWindow =>
        ReactiveCommand.CreateFromTask<Unit, EditAuthorizedKeysViewModel?>(
            async e =>
            {
                try
                {
                    var editAuthorizedKeysViewModel =
                        App.ServiceProvider.GetRequiredService<EditAuthorizedKeysViewModel>();
                    editAuthorizedKeysViewModel.SetConnectionAndKeys(ref _serverConnection, ref _sshKeys);
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
            var create = App.ServiceProvider.GetRequiredService<AddKeyWindowViewModel>();
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

    public ReactiveCommand<ISshKey, ISshKey?> DeleteKey =>
        ReactiveCommand.CreateFromTask<ISshKey, ISshKey?>(async u =>
        {
            var box = MessageBoxManager.GetMessageBoxStandard(
                string.Format(StringsAndTexts.MainWindowViewModelDeleteKeyTitleText, u.Filename,
                    u is ISshPublicKey up ? up.PrivateKey.Filename : "nothing else"),
                StringsAndTexts.MainWindowViewModelDeleteKeyQuestionText, ButtonEnum.YesNo, Icon.Question);
            var res = await box.ShowAsync();
            if (res != ButtonResult.Yes) return null;
            u.DeleteKey();
            SshKeys.Remove(u);
            EvaluateAppropriateIcon();
            return u;
        });

    public ReactiveCommand<ISshKey, ISshKey?> ConvertKey => ReactiveCommand.CreateFromTask<ISshKey, ISshKey?>(
        async key =>
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Convert key",
                "This will convert your key to the other Format (OpenSSH -> PuTTY) or (PuTTY -> OpenSSH).\nDo you want to delete the old file?",
                ButtonEnum.YesNoAbort, Icon.Warning);
            var errorBox = MessageBoxManager.GetMessageBoxStandard("Error converting key",
                "There was an error converting the keyfile. See the log for details.", ButtonEnum.Ok, Icon.Error);
            var oldIndex = SshKeys.IndexOf(key);


            var result = await box.ShowAsync();
            if (result is ButtonResult.Abort or ButtonResult.None) return key;

            var formatted = key is IPpkKey ppk
                ? ppk.Convert(SshKeyFormat.OpenSSH, _logger)
                : key.Convert(SshKeyFormat.PuTTYv3, _logger);
            if (formatted is null)
            {
                await errorBox.ShowAsync();
                return key;
            }

            SshKeys.Remove(key);
            SshKeys.Insert(oldIndex, formatted);

            if (result == ButtonResult.Yes) key.DeleteKey();

            return key;
        });

    public ReactiveCommand<Unit, ApplicationSettingsViewModel?> OpenAppSettings =>
        ReactiveCommand.CreateFromTask<Unit, ApplicationSettingsViewModel?>(async u =>
        {
            var vm = App.ServiceProvider.GetRequiredService<ApplicationSettingsViewModel>();
            vm.GetKeys(ref _sshKeys);
            var result = await ShowAppSettings.Handle(vm);
            return result ?? result;
        });

    public IServerConnection ServerConnection
    {
        get => _serverConnection;
        private set => this.RaiseAndSetIfChanged(ref _serverConnection, value);
    }

    public ObservableCollection<ISshKey?> SshKeys
    {
        get => _sshKeys;
        set => this.RaiseAndSetIfChanged(ref _sshKeys, value);
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