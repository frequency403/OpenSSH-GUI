﻿#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:45

#endregion

using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Interfaces.Credentials;
using OpenSSH_GUI.Core.Interfaces.Keys;
using ReactiveUI;

namespace OpenSSH_GUI.ViewModels;

public class EditSavedServerEntryViewModel(ILogger<EditSavedServerEntryViewModel> logger) : ViewModelBase(logger)
{
    private string _password = "";

    private ISshKey _selectedKey;
    public IConnectionCredentials CredentialsToEdit { get; set; }
    public ObservableCollection<ISshKey> Keys { get; set; }

    public ISshKey SelectedKey
    {
        get => _selectedKey;
        set => this.RaiseAndSetIfChanged(ref _selectedKey, value);
    }

    public string Password
    {
        get => _password;
        set => this.RaiseAndSetIfChanged(ref _password, value);
    }

    public bool IsPasswordKey => CredentialsToEdit is IPasswordConnectionCredentials;

    public ReactiveCommand<string, EditSavedServerEntryViewModel?> Close =>
        ReactiveCommand.Create<string, EditSavedServerEntryViewModel?>(e =>
        {
            if (!bool.Parse(e)) return null;
            if (CredentialsToEdit is IPasswordConnectionCredentials pwcc) pwcc.Password = Password;
            if (CredentialsToEdit is IKeyConnectionCredentials kcc) kcc.Key = SelectedKey;
            return this;
        });

    public void SetValues(ref ObservableCollection<ISshKey> keys, IConnectionCredentials credentials)
    {
        Keys = keys;
        CredentialsToEdit = credentials;
        if (CredentialsToEdit is IPasswordConnectionCredentials pwcc) Password = pwcc.Password;
        SelectedKey = CredentialsToEdit is IKeyConnectionCredentials kcc
            ? Keys.First(e => string.Equals(kcc.Key.Fingerprint, e.Fingerprint))
            : Keys.First();
    }
}