using System.Reactive;
using Avalonia.Media;
using Microsoft.Extensions.Logging;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using OpenSSH_GUI.Core.Interfaces.Services;
using OpenSSH_GUI.Core.Lib.Credentials;
using OpenSSH_GUI.Core.Lib.Keys;
using OpenSSH_GUI.Core.MVVM;
using ReactiveUI;

namespace OpenSSH_GUI.ViewModels;

public sealed class ConnectToServerViewModel : ViewModelBase<ConnectToServerViewModel>
{
    public ConnectToServerViewModel(ILogger<ConnectToServerViewModel>? logger,
        IServerConnectionService? serverConnectionService,
        ISshKeyManager? sshKeyManager) : base(logger)
    {
        ServerConnectionService = serverConnectionService;
        SshKeyManager = sshKeyManager;
        SelectedPublicKey = SshKeyManager.SshKeys.FirstOrDefault();
        UploadButtonEnabled = !TryingToConnect && ServerConnectionService.IsConnected;
        TestConnection = ReactiveCommand.CreateFromTask(TestConnectionAsync);
        ResetCommand = ReactiveCommand.Create(Reset);
    }

    public ConnectToServerViewModel() : this(null, null, null) { }

    public ReactiveCommand<Unit, Unit> TestConnection { get; }
    public ReactiveCommand<Unit, Unit> ResetCommand { get; }
    public ISshKeyManager SshKeyManager { get; }
    public IServerConnectionService ServerConnectionService { get; }

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

        var messageBox = MessageBoxManager.GetMessageBoxStandard(StringsAndTexts.Error, StatusButtonToolTip,
            ButtonEnum.Ok, Icon.Error);
        await messageBox.ShowAsync();
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