﻿#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 14.05.2024 - 00:05:30
// Last edit: 14.05.2024 - 03:05:24

#endregion

using System;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Interfaces.Keys;
using OpenSSH_GUI.Core.Interfaces.Misc;
using OpenSSH_GUI.Core.Models;
using ReactiveUI;

namespace OpenSSH_GUI.ViewModels;

public class EditAuthorizedKeysViewModel(ILogger<EditAuthorizedKeysViewModel> logger) : ViewModelBase(logger)
{
    private bool _addButtonEnabled;

    private ObservableCollection<ISshKey?> _publicKeys;

    private ISshKey? _selectedKey;

    private IServerConnection _serverConnection;

    public bool AddButtonEnabled
    {
        get => _addButtonEnabled;
        set => this.RaiseAndSetIfChanged(ref _addButtonEnabled, value);
    }

    public ISshKey? SelectedKey
    {
        get => _selectedKey;
        set
        {
            var valueFingerprint = value.Fingerprint;


            AddButtonEnabled =
                AuthorizedKeysFileRemote.AuthorizedKeys.Count(e =>
                    string.Equals(e.Fingerprint, value.Fingerprint, StringComparison.OrdinalIgnoreCase)) == 0;
            this.RaiseAndSetIfChanged(ref _selectedKey, value);
        }
    }

    public ObservableCollection<ISshKey> PublicKeys
    {
        get => _publicKeys;
        set => this.RaiseAndSetIfChanged(ref _publicKeys, value);
    }

    public bool KeyAddPossible => PublicKeys.Count > 0;

    public IServerConnection ServerConnection
    {
        get => _serverConnection;
        set => this.RaiseAndSetIfChanged(ref _serverConnection, value);
    }

    public IAuthorizedKeysFile AuthorizedKeysFileLocal { get; } =
        new AuthorizedKeysFile(SshConfigFiles.Authorized_Keys.GetPathOfFile());

    public IAuthorizedKeysFile AuthorizedKeysFileRemote { get; private set; }
    public ReactiveCommand<string, EditAuthorizedKeysViewModel> Submit { get; private set; }
    public ReactiveCommand<ISshKey, ISshKey?> AddKey { get; private set; }

    public void SetConnectionAndKeys(ref IServerConnection serverConnection,
        ref ObservableCollection<ISshKey?> keys)
    {
        _serverConnection = serverConnection;
        _publicKeys = keys;
        _selectedKey = PublicKeys.FirstOrDefault();
        AuthorizedKeysFileRemote = ServerConnection.GetAuthorizedKeysFromServer();
        Submit = ReactiveCommand.Create<string, EditAuthorizedKeysViewModel>(e =>
        {
            if (!bool.Parse(e)) return this;
            AuthorizedKeysFileLocal.PersistChangesInFile();
            ServerConnection.WriteAuthorizedKeysChangesToServer(AuthorizedKeysFileRemote);
            return this;
        });
        AddKey = ReactiveCommand.CreateFromTask<ISshKey, ISshKey?>(async e =>
        {
            await AuthorizedKeysFileRemote.AddAuthorizedKeyAsync(e);
            var keyExport = e.ExportAuthorizedKeyEntry();
            AddButtonEnabled =
                AuthorizedKeysFileRemote.AuthorizedKeys.All(key => key.Fingerprint != keyExport.Split(' ')[1]);
            return e;
        });
    }
}