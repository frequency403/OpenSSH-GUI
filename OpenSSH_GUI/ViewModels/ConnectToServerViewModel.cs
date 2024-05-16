#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:44

#endregion

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Media;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using OpenSSH_GUI.Core.Interfaces.Credentials;
using OpenSSH_GUI.Core.Interfaces.Keys;
using OpenSSH_GUI.Core.Interfaces.Misc;
using OpenSSH_GUI.Core.Interfaces.Settings;
using OpenSSH_GUI.Core.Lib.Misc;
using ReactiveUI;

namespace OpenSSH_GUI.ViewModels;

public class ConnectToServerViewModel : ViewModelBase
{
    private bool _authWithAllKeys;
    private bool _authWithPublicKey;

    private IEnumerable<IConnectionCredentials> _connectionCredentials;

    private string _hostName = "";

    private bool _keyComboBoxEnabled;
    private string _password = "";

    private bool _quickConnect;

    private IConnectionCredentials? _selectedConnection;

    private ISshKey? _selectedPublicKey;
    private IServerConnection _serverConnection = new ServerConnection("123", "123", "123");

    private IBrush _statusButtonBackground = Brushes.Gray;

    private string _statusButtonText = string.Format(StringsAndTexts.ConnectToServerStatusBase, StringsAndTexts.ConnectToServerStatusUnknown);

    private string _statusButtonToolTip = string.Format(StringsAndTexts.ConnectToServerStatusBase, StringsAndTexts.ConnectToServerStatusUntested);

    private bool _tryingToConnect;

    private bool _uploadButtonEnabled;
    private string _userName = "";
    private readonly bool firstCredentialSet = true;

    public ConnectToServerViewModel(ILogger<ConnectToServerViewModel> logger, ISettingsFile settings) :
        base(logger)
    {
        Settings = settings;
        ConnectionCredentials = Settings.LastUsedServers;
        SelectedConnection = ConnectionCredentials.FirstOrDefault();
        firstCredentialSet = false;
        UploadButtonEnabled = !TryingToConnect && ServerConnection.IsConnected;
        TestConnection = ReactiveCommand.CreateFromTask<Unit, Unit>(async e =>
        {
            if (QuickConnect)
            {
                TestQuickConnection(SelectedConnection).Wait();
                return e;
            }

            var task = Task.Run(() =>
            {
                try
                {
                    if (!ValidData) throw new ArgumentException(StringsAndTexts.ConnectToServerValidationError);
                    ServerConnection = AuthWithPublicKey
                        ? AuthWithAllKeys
                            ? new ServerConnection(Hostname, Username, PublicKeys)
                            : new ServerConnection(Hostname, Username, SelectedPublicKey)
                        : new ServerConnection(Hostname, Username, Password);
                    if (!ServerConnection.TestAndOpenConnection(out var ecException)) throw ecException;

                    return true;
                }
                catch (Exception exception)
                {
                    StatusButtonToolTip = exception.Message;
                    return false;
                }
            });
            TryingToConnect = true;
            if (await task)
            {
                StatusButtonText = string.Format(StringsAndTexts.ConnectToServerStatusBase, StringsAndTexts.ConnectToServerStatusSuccess);
                StatusButtonToolTip = string.Format(StringsAndTexts.ConnectToServerSshConnectionString, Username, Hostname);
                StatusButtonBackground = Brushes.Green;
            }
            else
            {
                StatusButtonText = string.Format(StringsAndTexts.ConnectToServerStatusBase, StringsAndTexts.ConnectToServerStatusFailed);
                StatusButtonBackground = Brushes.Red;
            }

            TryingToConnect = false;

            if (ServerConnection.IsConnected)
            {
                UploadButtonEnabled = !TryingToConnect && ServerConnection.IsConnected;
                return e;
            }

            var messageBox = MessageBoxManager.GetMessageBoxStandard(StringsAndTexts.Error, StatusButtonToolTip,
                ButtonEnum.Ok, Icon.Error);
            await messageBox.ShowAsync();

            return e;
        });
        ResetCommand = ReactiveCommand.Create<Unit, Unit>(e =>
        {
            Hostname = "";
            Username = "";
            Password = "";
            StatusButtonText = string.Format(StringsAndTexts.ConnectToServerStatusBase, StringsAndTexts.ConnectToServerStatusUnknown);
            StatusButtonToolTip = string.Format(StringsAndTexts.ConnectToServerStatusBase, StringsAndTexts.ConnectToServerStatusUntested);
            StatusButtonBackground = Brushes.Gray;
            ServerConnection = new ServerConnection("123", "123", "123");
            UploadButtonEnabled = !TryingToConnect && ServerConnection.IsConnected;
            return e;
        });
        SubmitConnection = ReactiveCommand.CreateFromTask<Unit, ConnectToServerViewModel>(async e =>
        {
            await App.ServiceProvider.GetRequiredService<IApplicationSettings>()
                .AddKnownServerToFileAsync(ServerConnection.ConnectionCredentials);
            return this;
        });
    }

    public bool QuickConnectAvailable => ConnectionCredentials.Any();

    public IEnumerable<IConnectionCredentials> ConnectionCredentials
    {
        get => _connectionCredentials;
        set => this.RaiseAndSetIfChanged(ref _connectionCredentials, value);
    }

    public IConnectionCredentials? SelectedConnection
    {
        get => _selectedConnection;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedConnection, value);
            TestQuickConnection(value).Wait();
        }
    }

    public bool QuickConnect
    {
        get => _quickConnect;
        set => this.RaiseAndSetIfChanged(ref _quickConnect, value);
    }

    public ISettingsFile Settings { get; }

    public IServerConnection ServerConnection
    {
        get => _serverConnection;
        private set => this.RaiseAndSetIfChanged(ref _serverConnection, value);
    }

    private bool ValidData => SelectedPublicKey is null
        ? Hostname != "" && Username != "" && Password != ""
        : Hostname != "" && Username != "";

    public bool UploadButtonEnabled
    {
        get => _uploadButtonEnabled;
        set => this.RaiseAndSetIfChanged(ref _uploadButtonEnabled, value);
    }

    public bool AuthWithPublicKey
    {
        get => _authWithPublicKey;
        set
        {
            this.RaiseAndSetIfChanged(ref _authWithPublicKey, value);
            UpdateComboBoxState();
        }
    }

    public bool AuthWithAllKeys
    {
        get => _authWithAllKeys;
        set
        {
            this.RaiseAndSetIfChanged(ref _authWithAllKeys, value);
            UpdateComboBoxState();
        }
    }

    public ISshKey? SelectedPublicKey
    {
        get => _selectedPublicKey;
        set => this.RaiseAndSetIfChanged(ref _selectedPublicKey, value);
    }

    public ObservableCollection<ISshKey?> PublicKeys { get; private set; }

    public string Hostname
    {
        get => _hostName;
        set => this.RaiseAndSetIfChanged(ref _hostName, value);
    }

    public string Username
    {
        get => _userName;
        set => this.RaiseAndSetIfChanged(ref _userName, value);
    }

    public string Password
    {
        get => _password;
        set => this.RaiseAndSetIfChanged(ref _password, value);
    }

    public bool TryingToConnect
    {
        get => _tryingToConnect;
        set => this.RaiseAndSetIfChanged(ref _tryingToConnect, value);
    }

    public string StatusButtonToolTip
    {
        get => _statusButtonToolTip;
        set => this.RaiseAndSetIfChanged(ref _statusButtonToolTip, value);
    }

    public string StatusButtonText
    {
        get => _statusButtonText;
        set => this.RaiseAndSetIfChanged(ref _statusButtonText, value);
    }

    public IBrush StatusButtonBackground
    {
        get => _statusButtonBackground;
        set => this.RaiseAndSetIfChanged(ref _statusButtonBackground, value);
    }

    public bool KeyComboBoxEnabled
    {
        get => _keyComboBoxEnabled;
        set => this.RaiseAndSetIfChanged(ref _keyComboBoxEnabled, value);
    }

    public ReactiveCommand<Unit, ConnectToServerViewModel> SubmitConnection { get; }
    public ReactiveCommand<Unit, Unit> TestConnection { get; }
    public ReactiveCommand<Unit, Unit> ResetCommand { get; }

    private bool TestConnectionInternal(IConnectionCredentials credentials)
    {
        try
        {
            ServerConnection = new ServerConnection(credentials);
            if (!ServerConnection.TestAndOpenConnection(out var ecException)) throw ecException;
            return true;
        }
        catch (Exception exception)
        {
            StatusButtonToolTip = exception.Message;
            return false;
        }
    }

    private async Task TestQuickConnection(IConnectionCredentials? credentials)
    {
        if (firstCredentialSet) return;
        TryingToConnect = true;
        if (TestConnectionInternal(credentials))
        {
            StatusButtonText = string.Format(StringsAndTexts.ConnectToServerStatusBase, StringsAndTexts.ConnectToServerStatusSuccess);
            StatusButtonToolTip = string.Format(StringsAndTexts.ConnectToServerSshConnectionString, Username, Hostname);
            StatusButtonBackground = Brushes.Green;
        }
        else
        {
            StatusButtonText = string.Format(StringsAndTexts.ConnectToServerStatusBase, StringsAndTexts.ConnectToServerStatusFailed);
            StatusButtonBackground = Brushes.Red;
        }

        TryingToConnect = false;

        if (ServerConnection.IsConnected)
        {
            UploadButtonEnabled = !TryingToConnect && ServerConnection.IsConnected;
            return;
        }

        var messageBox = MessageBoxManager.GetMessageBoxStandard(StringsAndTexts.Error, StatusButtonToolTip,
            ButtonEnum.Ok, Icon.Error);
        await messageBox.ShowAsync();
    }

    public void UpdateComboBoxState()
    {
        KeyComboBoxEnabled = !ServerConnection.IsConnected && AuthWithPublicKey && !AuthWithAllKeys;
    }

    public void SetKeys(ref ObservableCollection<ISshKey?> currentKeys)
    {
        PublicKeys = new ObservableCollection<ISshKey>(currentKeys.Where(e => e is not null && ((e.HasPassword && !e.NeedPassword) || (!e.HasPassword && !e.NeedPassword))));
        _selectedPublicKey = PublicKeys.FirstOrDefault();
    }
}