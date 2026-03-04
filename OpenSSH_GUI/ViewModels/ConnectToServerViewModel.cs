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
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using OpenSSH_GUI.Core.Database.DTO;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Interfaces.Credentials;
using OpenSSH_GUI.Core.Interfaces.Keys;
using OpenSSH_GUI.Core.Interfaces.Misc;
using OpenSSH_GUI.Core.Lib.Misc;
using OpenSSH_GUI.Core.MVVM;
using ReactiveUI;

namespace OpenSSH_GUI.ViewModels;

public sealed class ConnectToServerViewModel : ViewModelBase<ConnectToServerViewModel>
{
    private readonly bool _firstCredentialSet;

    private List<IConnectionCredentials> _connectionCredentials;

    private ISshKey? _selectedPublicKey;

    public ConnectToServerViewModel(ref ObservableCollection<ISshKey?> keys,
        List<IConnectionCredentials> credentialsList)
    {
        PublicKeys = new ObservableCollection<ISshKey?>(keys.Where(e => e is not null && !e.NeedPassword));
        _selectedPublicKey = PublicKeys.FirstOrDefault();
        _connectionCredentials = credentialsList;
        _firstCredentialSet = false;
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
            StatusButtonText = string.Format(StringsAndTexts.ConnectToServerStatusBase,
                StringsAndTexts.ConnectToServerStatusUnknown);
            StatusButtonToolTip = string.Format(StringsAndTexts.ConnectToServerStatusBase,
                StringsAndTexts.ConnectToServerStatusUntested);
            StatusButtonBackground = Brushes.Gray;
            ServerConnection = new ServerConnection("123", "123", "123");
            UploadButtonEnabled = !TryingToConnect && ServerConnection.IsConnected;
            return e;
        });
        BooleanSubmit = ReactiveCommand.CreateFromTask<bool, ConnectToServerViewModel?>(
            async e =>
            {
                ServerConnection.ConnectionCredentials.Id =
                    (await ServerConnection.ConnectionCredentials.SaveDtoInDatabase() ?? new ConnectionCredentialsDto())
                    .Id;
                return this;
            });
    }

    public bool QuickConnectAvailable => ConnectionCredentials.Any();

    public List<IConnectionCredentials> ConnectionCredentials
    {
        get => _connectionCredentials;
        set => this.RaiseAndSetIfChanged(ref _connectionCredentials, value);
    }

    public IConnectionCredentials? SelectedConnection
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            TestQuickConnection(value).Wait();
        }
    }

    public bool QuickConnect
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public IServerConnection ServerConnection
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    } = new ServerConnection("123", "123", "123");

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

    public ISshKey? SelectedPublicKey
    {
        get => _selectedPublicKey;
        set => this.RaiseAndSetIfChanged(ref _selectedPublicKey, value);
    }

    public ObservableCollection<ISshKey?> PublicKeys { get; }

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
        if (_firstCredentialSet) return;
        TryingToConnect = true;
        if (TestConnectionInternal(credentials))
        {
            StatusButtonText = string.Format(StringsAndTexts.ConnectToServerStatusBase,
                StringsAndTexts.ConnectToServerStatusSuccess);
            StatusButtonToolTip = string.Format(StringsAndTexts.ConnectToServerSshConnectionString, Username, Hostname);
            StatusButtonBackground = Brushes.Green;
        }
        else
        {
            StatusButtonText = string.Format(StringsAndTexts.ConnectToServerStatusBase,
                StringsAndTexts.ConnectToServerStatusFailed);
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
}