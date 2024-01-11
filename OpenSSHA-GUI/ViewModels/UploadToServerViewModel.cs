using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reactive;
using System.Threading;
using Avalonia.Media;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using OpenSSHALib.Lib;
using OpenSSHALib.Models;
using ReactiveUI;
using ReactiveUI.Validation.Abstractions;
using ReactiveUI.Validation.Contexts;

namespace OpenSSHA_GUI.ViewModels;

public class UploadToServerViewModel : ViewModelBase, IValidatableViewModel
{
    public UploadToServerViewModel() : this([], new ServerConnection("123", "123", "123")){}
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
                else if(!ServerConnection.WriteAuthorizedKeysChangesToServer(authorizedKeys))
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

                var messageBox = MessageBoxManager.GetMessageBoxStandard($"Upload result of key {SelectedPublicKey.Filename}", messageBoxText, ButtonEnum.Ok, messageBoxIcon);
                await messageBox.ShowAsync();
                return this;
            });
    }

    public string Hostname { get; set; } = "";
    public string User { get; set; } = "";
    public string Password { get; set; } = "";
    private bool ConnectionSuccessful { get; set; } = false;
    
    
    private bool _uploadButtonEnabled = false;
    public bool UploadButtonEnabled
    {
        get => _uploadButtonEnabled;
        set => this.RaiseAndSetIfChanged(ref _uploadButtonEnabled, value);
    }

    private void EvaluateEnabledState()
    {
        UploadButtonEnabled = Hostname is not "" && User is not "" && Password is not "" && ConnectionSuccessful;
    }
    
    public ReactiveCommand<Unit, Unit> TestConnection { get; }
    
    private string _statusButtonToolTip = "Status not yet tested";
    public string StatusButtonToolTip
    {
        get => _statusButtonToolTip;
        set => this.RaiseAndSetIfChanged(ref _statusButtonToolTip, value);
    }
    
    private string _statusButtonText = "Status: Unknown";
    public string StatusButtonText
    {
        get => _statusButtonText;
        set => this.RaiseAndSetIfChanged(ref _statusButtonText, value);
    }
    
    private IBrush _statusButtonBackground = Brushes.Gray;
    public IBrush StatusButtonBackground
    {
        get => _statusButtonBackground;
        set => this.RaiseAndSetIfChanged(ref _statusButtonBackground, value);
    }
    
    public ServerConnection ServerConnection { get; }
    public SshPublicKey SelectedPublicKey { get; set; }
    public ObservableCollection<SshPublicKey> Keys { get; }
    public ValidationContext ValidationContext { get; } = new();
    public ReactiveCommand<Unit, UploadToServerViewModel> UploadAction { get; }
    public ReactiveCommand<Unit, Unit> ResetCommand { get; }
}