﻿#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 14.05.2024 - 00:05:30
// Last edit: 14.05.2024 - 03:05:25

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
using OpenSSH_GUI.Assets;
using OpenSSH_GUI.Core.Interfaces.Credentials;
using OpenSSH_GUI.Core.Interfaces.Keys;
using OpenSSH_GUI.Core.Interfaces.Misc;
using OpenSSH_GUI.Core.Interfaces.Settings;
using OpenSSH_GUI.Core.Lib;
using OpenSSH_GUI.Core.Lib.Misc;
using ReactiveUI;

namespace OpenSSH_GUI.ViewModels;

public class ConnectToServerViewModel : ViewModelBase
{
    private bool _authWithAllKeys;
    private bool _authWithPublicKey;

    private string _hostName = "";

    private bool _keyComboBoxEnabled;
    private string _password = "";

    private ISshKey? _selectedPublicKey;
    private IServerConnection _serverConnection = new ServerConnection("123", "123", "123");

    private IBrush _statusButtonBackground = Brushes.Gray;

    private string _statusButtonText = "Status: Unknown";

    private string _statusButtonToolTip = "Status not yet tested";

    private bool _tryingToConnect;

    private bool _uploadButtonEnabled;
    private string _userName = "";
    private bool firstCredentialSet = true;

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
            StatusButtonText = "Status: success";
            StatusButtonToolTip = $"Connected to ssh://{Username}@{Hostname}";
            StatusButtonBackground = Brushes.Green;
        }
        else
        {
            StatusButtonText = "Status: failed!";
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

    public bool QuickConnectAvailable => ConnectionCredentials.Any();
    
    public ConnectToServerViewModel(ILogger<ConnectToServerViewModel> logger, IApplicationSettings settings) :
        base(logger)
    {
        Settings = settings;
        ConnectionCredentials = Settings.Settings.LastUsedServers;
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
                    if (!ValidData) throw new ArgumentException("Missing hostname/ip, username or password!");
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
                StatusButtonText = "Status: success";
                StatusButtonToolTip = $"Connected to ssh://{Username}@{Hostname}";
                StatusButtonBackground = Brushes.Green;
            }
            else
            {
                StatusButtonText = "Status: failed!";
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
            StatusButtonText = "Status: unknown";
            StatusButtonToolTip = "Status not yet tested!";
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

    private IEnumerable<IConnectionCredentials> _connectionCredentials;

    public IEnumerable<IConnectionCredentials> ConnectionCredentials
    {
        get => _connectionCredentials;
        set => this.RaiseAndSetIfChanged(ref _connectionCredentials, value);
    }

    private IConnectionCredentials? _selectedConnection;

    public IConnectionCredentials? SelectedConnection
    {
        get => _selectedConnection;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedConnection, value);
            TestQuickConnection(value).Wait();
        }
    }
    
    private bool _quickConnect = false;

    public bool QuickConnect
    {
        get => _quickConnect;
        set => this.RaiseAndSetIfChanged(ref _quickConnect, value);
    }
    
    public IApplicationSettings Settings { get; }

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

    public void UpdateComboBoxState()
    {
        KeyComboBoxEnabled = !ServerConnection.IsConnected && AuthWithPublicKey && !AuthWithAllKeys;
    }

    public void SetKeys(ref ObservableCollection<ISshKey?> currentKeys)
    {
        PublicKeys = currentKeys;
        _selectedPublicKey = PublicKeys.FirstOrDefault();
    }
}