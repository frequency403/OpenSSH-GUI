// File Created by: Oliver Schantz
// Created: 14.05.2024 - 13:05:53
// Last edit: 14.05.2024 - 13:05:53

using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Interfaces.Credentials;
using OpenSSH_GUI.Core.Interfaces.Keys;
using ReactiveUI;

namespace OpenSSH_GUI.ViewModels;

public class EditSavedServerEntryViewModel(ILogger<EditSavedServerEntryViewModel> logger) : ViewModelBase(logger)
{
    public IConnectionCredentials CredentialsToEdit { get; set; }
    public ObservableCollection<ISshKey> Keys { get; set; }

    private ISshKey _selectedKey;

    public ISshKey SelectedKey
    {
        get => _selectedKey;
        set => this.RaiseAndSetIfChanged(ref _selectedKey, value);
    }

    private string _password = "";

    public string Password
    {
        get => _password;
        set => this.RaiseAndSetIfChanged(ref _password, value);
    }

    public bool IsPasswordKey => CredentialsToEdit is IPasswordConnectionCredentials;

    public void SetValues(ref ObservableCollection<ISshKey> keys, IConnectionCredentials credentials)
    {
        Keys = keys;
        CredentialsToEdit = credentials;
        if (CredentialsToEdit is IPasswordConnectionCredentials pwcc) Password = pwcc.Password;
        SelectedKey = CredentialsToEdit is IKeyConnectionCredentials kcc
            ? Keys.First(e => string.Equals(kcc.Key.Fingerprint, e.Fingerprint))
            : Keys.First();
    }

    public ReactiveCommand<string, EditSavedServerEntryViewModel?> Close =>
        ReactiveCommand.Create<string, EditSavedServerEntryViewModel?>(e =>
        {
            if (!bool.Parse(e)) return null;
            if (CredentialsToEdit is IPasswordConnectionCredentials pwcc) pwcc.Password = Password;
            if (CredentialsToEdit is IKeyConnectionCredentials kcc) kcc.Key = SelectedKey;
            return this;
        });
}