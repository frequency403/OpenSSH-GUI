using System.Reactive;
using Avalonia.Media;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Lib.Credentials;
using OpenSSH_GUI.Core.Lib.Keys;
using OpenSSH_GUI.Core.MVVM;
using OpenSSH_GUI.Core.Services;
using OpenSSH_GUI.Dialogs.Enums;
using OpenSSH_GUI.Dialogs.Interfaces;
using OpenSSH_GUI.Resources;
using ReactiveUI;

namespace OpenSSH_GUI.ViewModels;

[UsedImplicitly]
public sealed class ConnectToServerViewModel : ViewModelBase<ConnectToServerViewModel>
{
    private readonly IMessageBoxProvider? _messageBoxProvider;

    public ConnectToServerViewModel(ILogger<ConnectToServerViewModel> logger,
        ServerConnectionService serverConnectionService,
        IMessageBoxProvider messageBoxProvider,
        SshKeyManager sshKeyManager) : base(logger)
    {
        _messageBoxProvider = messageBoxProvider;
        ServerConnectionService = serverConnectionService;
        SshKeyManager = sshKeyManager;
        SelectedPublicKey = SshKeyManager.SshKeys.FirstOrDefault();
        UploadButtonEnabled = !TryingToConnect && ServerConnectionService.IsConnected;
        TestConnection = ReactiveCommand.CreateFromTask(TestConnectionAsync);
        ResetCommand = ReactiveCommand.Create(Reset);
    }

    public ReactiveCommand<Unit, Unit> TestConnection { get; }
    public ReactiveCommand<Unit, Unit> ResetCommand { get; }
    public SshKeyManager SshKeyManager { get; }
    public ServerConnectionService ServerConnectionService { get; }

    private bool ValidData => SelectedPublicKey is null
        ? Hostname != "" && Username != "" && Password != ""
        : Hostname != "" && Username != "";

    public bool UploadButtonEnabled
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public bool AuthWithPublicKey
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            UpdateComboBoxState();
        }
    }

    public bool AuthWithAllKeys
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            UpdateComboBoxState();
        }
    }

    public SshKeyFile? SelectedPublicKey
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string Hostname
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = "";

    public string Username
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = "";

    public string Password
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = "";

    public bool TryingToConnect
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string StatusButtonToolTip
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = string.Format(StringsAndTexts.ConnectToServerStatusBase,
        StringsAndTexts.ConnectToServerStatusUntested);

    public string StatusButtonText
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = string.Format(StringsAndTexts.ConnectToServerStatusBase,
        StringsAndTexts.ConnectToServerStatusUnknown);

    public IBrush StatusButtonBackground
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = Brushes.Gray;

    public bool KeyComboBoxEnabled
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    private async Task TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ValidData) throw new ArgumentException(StringsAndTexts.ConnectToServerValidationError);
            TryingToConnect = true;
            if (AuthWithPublicKey)
                await ServerConnectionService.EstablishConnection(
                    new KeyConnectionCredentials(Hostname, Username, SelectedPublicKey), cancellationToken);
            else if (AuthWithAllKeys)
                await ServerConnectionService.EstablishConnection(
                    new MultiKeyConnectionCredentials(Hostname, Username, SshKeyManager.SshKeys),
                    cancellationToken);
            else
                await ServerConnectionService.EstablishConnection(
                    new PasswordConnectionCredentials(Hostname, Username, Password), cancellationToken);
        }
        catch (Exception exception)
        {
            StatusButtonToolTip = exception.Message;
        }

        if (ServerConnectionService.IsConnected)
        {
            StatusButtonText = string.Format(StringsAndTexts.ConnectToServerStatusBase,
                StringsAndTexts.ConnectToServerStatusSuccess);
            StatusButtonToolTip =
                string.Format(StringsAndTexts.ConnectToServerSshConnectionString, Username, Hostname);
            StatusButtonBackground = Brushes.Green;
        }
        else
        {
            StatusButtonText = string.Format(StringsAndTexts.ConnectToServerStatusBase,
                StringsAndTexts.ConnectToServerStatusFailed);
            StatusButtonBackground = Brushes.Red;
        }

        TryingToConnect = false;

        if (ServerConnectionService.IsConnected)
        {
            UploadButtonEnabled = !TryingToConnect && ServerConnectionService.IsConnected;
            return;
        }

        await _messageBoxProvider!.ShowMessageBoxAsync(StringsAndTexts.Error, StatusButtonToolTip, MessageBoxButtons.Ok,
            MessageBoxIcon.Error);
    }

    private void Reset()
    {
        Hostname = string.Empty;
        Username = string.Empty;
        Password = string.Empty;
        StatusButtonText = string.Format(StringsAndTexts.ConnectToServerStatusBase,
            StringsAndTexts.ConnectToServerStatusUnknown);
        StatusButtonToolTip = string.Format(StringsAndTexts.ConnectToServerStatusBase,
            StringsAndTexts.ConnectToServerStatusUntested);
        StatusButtonBackground = Brushes.Gray;
        UploadButtonEnabled = !TryingToConnect && ServerConnectionService.IsConnected;
    }

    private void UpdateComboBoxState()
    {
        KeyComboBoxEnabled = !ServerConnectionService.IsConnected && AuthWithPublicKey && !AuthWithAllKeys;
    }
}