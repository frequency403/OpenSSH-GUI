using System.Reactive;
using Avalonia.Media;
using Microsoft.Extensions.Logging;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using OpenSSH_GUI.Core.Interfaces;
using OpenSSH_GUI.Core.Lib.Credentials;
using OpenSSH_GUI.Core.Lib.Keys;
using OpenSSH_GUI.Core.MVVM;
using OpenSSH_GUI.Core.Services;
using ReactiveUI;

namespace OpenSSH_GUI.ViewModels;

public sealed class ConnectToServerViewModel(
    ILogger<ConnectToServerViewModel> logger,
    IServerConnectionService serverConnectionService,
    KeyLocatorService keyLocatorService) : ViewModelBase<ConnectToServerViewModel>(logger)
{
    private SshKeyFile? _selectedPublicKey;
    public KeyLocatorService KeyLocatorService => keyLocatorService;
    public IServerConnectionService ServerConnectionService => serverConnectionService;

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
        get => _selectedPublicKey;
        set => this.RaiseAndSetIfChanged(ref _selectedPublicKey, value);
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

    public ReactiveCommand<Unit, Unit> TestConnection { get; private set; }
    public ReactiveCommand<Unit, Unit> ResetCommand { get; private set; }

    public override async ValueTask InitializeAsync(
        IInitializerParameters<ConnectToServerViewModel>? initializerParameters = null,
        CancellationToken cancellationToken = default)
    {
        _selectedPublicKey = keyLocatorService.SshKeys.FirstOrDefault();
        UploadButtonEnabled = !TryingToConnect && serverConnectionService.IsConnected;
        TestConnection = ReactiveCommand.CreateFromTask<Unit, Unit>(async e =>
        {
            try
            {
                if (!ValidData) throw new ArgumentException(StringsAndTexts.ConnectToServerValidationError);
                TryingToConnect = true;
                if (AuthWithPublicKey)
                    await serverConnectionService.EstablishConnection(
                        new KeyConnectionCredentials(Hostname, Username, SelectedPublicKey), cancellationToken);
                else if (AuthWithAllKeys)
                    await serverConnectionService.EstablishConnection(
                        new MultiKeyConnectionCredentials(Hostname, Username, keyLocatorService.SshKeys),
                        cancellationToken);
                else
                    await serverConnectionService.EstablishConnection(
                        new PasswordConnectionCredentials(Hostname, Username, Password), cancellationToken);
            }
            catch (Exception exception)
            {
                StatusButtonToolTip = exception.Message;
            }

            if (!serverConnectionService.IsConnected)
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

            if (serverConnectionService.IsConnected)
            {
                UploadButtonEnabled = !TryingToConnect && serverConnectionService.IsConnected;
                return e;
            }

            var messageBox = MessageBoxManager.GetMessageBoxStandard(StringsAndTexts.Error, StatusButtonToolTip,
                ButtonEnum.Ok, Icon.Error);
            await messageBox.ShowAsync();

            return e;
        });
        ResetCommand = ReactiveCommand.Create<Unit, Unit>(e =>
        {
            Hostname = string.Empty;
            Username = string.Empty;
            Password = string.Empty;
            StatusButtonText = string.Format(StringsAndTexts.ConnectToServerStatusBase,
                StringsAndTexts.ConnectToServerStatusUnknown);
            StatusButtonToolTip = string.Format(StringsAndTexts.ConnectToServerStatusBase,
                StringsAndTexts.ConnectToServerStatusUntested);
            StatusButtonBackground = Brushes.Gray;
            UploadButtonEnabled = !TryingToConnect && serverConnectionService.IsConnected;
            return e;
        });
        await base.InitializeAsync(initializerParameters, cancellationToken);
    }

    private void UpdateComboBoxState()
    {
        KeyComboBoxEnabled = !serverConnectionService.IsConnected && AuthWithPublicKey && !AuthWithAllKeys;
    }
}