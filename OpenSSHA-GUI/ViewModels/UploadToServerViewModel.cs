using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reactive;
using System.Threading;
using Avalonia.Media;
using MsBox.Avalonia;
using MsBox.Avalonia.Base;
using MsBox.Avalonia.Enums;
using OpenSSHALib.Lib;
using OpenSSHALib.Models;
using ReactiveUI;
using ReactiveUI.Validation.Abstractions;
using ReactiveUI.Validation.Contexts;
using Renci.SshNet;

namespace OpenSSHA_GUI.ViewModels;

public class UploadToServerViewModel : ViewModelBase, IValidatableViewModel
{
    public UploadToServerViewModel() : this([], new ServerConnection("123", "123", "123"))
    {
    }

    public UploadToServerViewModel(ObservableCollection<SshPublicKey> keys, ServerConnection serverConnection)
    {
        ServerConnection = serverConnection;
        Keys = keys;
        SelectedPublicKey = Keys.First();
        UploadAction = ReactiveCommand.CreateFromTask<Unit, UploadToServerViewModel>(
            async e =>
            {
                var messageBoxText = "Upload successful!";
                var messageBoxIcon = Icon.Success;

                var authorizedKeys = ServerConnection.GetAuthorizedKeysFromServer();
                if (!await authorizedKeys.AddAuthorizedKeyAsync(SelectedPublicKey))
                {
                    messageBoxIcon = Icon.Error;
                    messageBoxText = "Error adding the key to the collection!";
                }
                else if (!ServerConnection.WriteAuthorizedKeysChangesToServer(authorizedKeys))
                {
                    messageBoxIcon = Icon.Error;
                    messageBoxText = "Error saving the key on the server!";
                }

                // if (ServerCommunicator.TryOpenSshConnection(Hostname, User, Password, out var client, out var errorMessage))
                // {
                //     try
                //     {
                //         messageBoxText = await client.PutKeyToServer(SelectedPublicKey);
                //     }
                //     catch (Exception exception)
                //     {
                //         messageBoxText = exception.Message;
                //         messageBoxIcon = Icon.Error;
                //     }
                // }
                // else
                // {
                //     messageBoxText = errorMessage;
                //     messageBoxIcon = Icon.Error;
                // }

                var messageBox = MessageBoxManager.GetMessageBoxStandard(
                    $"Upload result of key {SelectedPublicKey.Filename}", messageBoxText, ButtonEnum.Ok,
                    messageBoxIcon);
                await messageBox.ShowAsync();
                if (messageBoxIcon == Icon.Error) return this;
                var askForTrial = MessageBoxManager.GetMessageBoxStandard($"Authenticate with public key",
                    "Try to connect via uploaded public key to verify functionality?", ButtonEnum.YesNo, Icon.Question);
                var questionResult = await askForTrial.ShowAsync();
                if (questionResult == ButtonResult.No) return this;
                IMsBox<ButtonResult>? resultMessage = null;
                try
                {
                    var sshClient = new SshClient(new PrivateKeyConnectionInfo(ServerConnection.Hostname, ServerConnection.Username, new PrivateKeyFile(SelectedPublicKey.PrivateKey.AbsoluteFilePath)));
                    sshClient.Connect();
                        resultMessage = sshClient.IsConnected ? MessageBoxManager.GetMessageBoxStandard("Connection successful!", "Connection successfully established trough private key!", ButtonEnum.Ok, Icon.Success) : 
                            MessageBoxManager.GetMessageBoxStandard("Private Key connection failed",
                            "Private key connection failed", ButtonEnum.Ok, Icon.Error);
                        sshClient.Disconnect();
                }
                catch (Exception exception)
                {
                    resultMessage = MessageBoxManager.GetMessageBoxStandard(StringsAndTexts.Error, exception.Message,
                        ButtonEnum.Ok, Icon.Error);
                }
                if (resultMessage is not null) await resultMessage.ShowAsync();
                return this;
            });
    }

    public ServerConnection ServerConnection { get; }
    public SshPublicKey SelectedPublicKey { get; set; }
    public ObservableCollection<SshPublicKey> Keys { get; }
    public ValidationContext ValidationContext { get; } = new();
    public ReactiveCommand<Unit, UploadToServerViewModel> UploadAction { get; }

}
