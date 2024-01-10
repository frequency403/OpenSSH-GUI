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
    public UploadToServerViewModel() : this([]){}
    public UploadToServerViewModel(ObservableCollection<SshPublicKey> keys)
    {
        Keys = keys;
        SelectedPublicKey = Keys.First();
        UploadAction = ReactiveCommand.CreateFromTask<Unit, UploadToServerViewModel>(
            async e =>
            {
                string messageBoxText;
                var messageBoxIcon = Icon.Success;
                if(Hostname is "" || User is "" || Password is "") messageBoxIcon = Icon.Error;
                if (ServerCommunicator.TryOpenSshConnection(Hostname, User, Password, out var client, out var errorMessage))
                {
                    try
                    {
                        messageBoxText = await client.PutKeyToServer(SelectedPublicKey);
                    }
                    catch (Exception exception)
                    {
                        messageBoxText = exception.Message;
                        messageBoxIcon = Icon.Error;
                    }
                }
                else
                {
                    messageBoxText = errorMessage;
                    messageBoxIcon = Icon.Error;
                }

                var messageBox = MessageBoxManager.GetMessageBoxStandard($"Upload result of key {SelectedPublicKey.Filename}", messageBoxText, ButtonEnum.Ok, messageBoxIcon);
                await messageBox.ShowAsync();
                return this;
            });
        TestConnection  = ReactiveCommand.CreateFromTask<Unit, Unit>(async e =>
        {
            var toolTipMessage = "Missing host, user or password!";
            if(Hostname is "" || User is "" || Password is "") goto Failed;
            if (ServerCommunicator.TestConnection(Hostname, User, Password, out var message))
            {
                StatusButtonBackground = Brushes.LimeGreen;
                StatusButtonText = "Status: success";
                StatusButtonToolTip = $"Connection successfully established for ssh://{User}@{Hostname}";
                ConnectionSuccessful = true;
                EvaluateEnabledState();
                return e;
            }
            toolTipMessage = message;
            Failed:
            StatusButtonBackground = Brushes.IndianRed;
            StatusButtonText = "Status: failed";
            StatusButtonToolTip = toolTipMessage;
            Hostname = "";
            User = "";
            Password = "";
            EvaluateEnabledState();
            return e;
        });
        ResetCommand = ReactiveCommand.Create<Unit, Unit>(e =>
        {
            Hostname = "";
            User = "";
            Password = "";
            ConnectionSuccessful = false;
            SelectedPublicKey = Keys.First();
            StatusButtonText = "Status: unknown";
            StatusButtonToolTip = "Status not yet tested!";
            StatusButtonBackground = Brushes.Gray;
            EvaluateEnabledState();
            return e;
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
    
    public SshPublicKey SelectedPublicKey { get; set; }
    public ObservableCollection<SshPublicKey> Keys { get; }
    public ValidationContext ValidationContext { get; } = new();
    public ReactiveCommand<Unit, UploadToServerViewModel> UploadAction { get; }
    public ReactiveCommand<Unit, Unit> ResetCommand { get; }
}