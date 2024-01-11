using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using OpenSSHALib.Lib;
using OpenSSHALib.Models;
using ReactiveUI;

namespace OpenSSHA_GUI.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public readonly Interaction<AddKeyWindowViewModel, AddKeyWindowViewModel?> ShowCreate = new();
    public readonly Interaction<EditKnownHostsViewModel, EditKnownHostsViewModel?> ShowEditKnownHosts = new();
    public readonly Interaction<ExportWindowViewModel, ExportWindowViewModel?> ShowExportWindow = new();
    public readonly Interaction<UploadToServerViewModel, UploadToServerViewModel?> ShowUploadToServer = new();
    public readonly Interaction<EditAuthorizedKeysViewModel, EditAuthorizedKeysViewModel?> ShowEditAuthorizedKeys = new();
    public readonly Interaction<ConnectToServerViewModel, ConnectToServerViewModel?> ShowConnectToServerWindow = new();
    
    
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
    
    public ReactiveCommand<Unit, ConnectToServerViewModel?> OpenConnectToServerWindow => ReactiveCommand.CreateFromTask<Unit, ConnectToServerViewModel?>(async e =>
    {
        var connectToServer = new ConnectToServerViewModel();
        var windowResult = await ShowConnectToServerWindow.Handle(connectToServer);
        if (windowResult is not null) ServerConnection = windowResult.ServerConnection;
        return windowResult;
    });
    
    public ReactiveCommand<Unit, EditKnownHostsViewModel?> OpenEditKnownHostsWindow =>
        ReactiveCommand.CreateFromTask<Unit, EditKnownHostsViewModel?>(async e =>
        {
            var editKnownHosts = new EditKnownHostsViewModel(ServerConnection);
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


    public ReactiveCommand<Unit, UploadToServerViewModel?> OpenUploadToServerWindow => ReactiveCommand.CreateFromTask<Unit, UploadToServerViewModel?>(async e =>
    {
        var uploadViewModel = new UploadToServerViewModel(_sshKeys, ServerConnection);
        var result = await ShowUploadToServer.Handle(uploadViewModel);
        return result;
    });

    public ReactiveCommand<Unit, EditAuthorizedKeysViewModel?> OpenEditAuthorizedKeysWindow =>
        ReactiveCommand.CreateFromTask<Unit, EditAuthorizedKeysViewModel?>(
            async e =>
            {
                try
                {
                    var editAuthorizedKeysViewModel = new EditAuthorizedKeysViewModel(ServerConnection);
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
            return u;
        });

    private ServerConnection _serverConnection = new ("123", "123", "123");
    public ServerConnection ServerConnection
    {
        get => _serverConnection;
        set => this.RaiseAndSetIfChanged(ref _serverConnection, value);
    }
    
    private ObservableCollection<SshPublicKey> _sshKeys = new(DirectoryCrawler.GetAllKeys());
    public ObservableCollection<SshPublicKey> SshKeys
    {
        get => _sshKeys;
        set => this.RaiseAndSetIfChanged(ref _sshKeys, value);
    }
}