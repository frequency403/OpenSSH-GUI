#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:43

#endregion

using System.Collections.ObjectModel;
using System.Linq;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Interfaces.AuthorizedKeys;
using OpenSSH_GUI.Core.Interfaces.Keys;
using OpenSSH_GUI.Core.Interfaces.Misc;
using OpenSSH_GUI.Core.Lib.AuthorizedKeys;
using ReactiveUI;

namespace OpenSSH_GUI.ViewModels;

public class EditAuthorizedKeysViewModel : ViewModelBase<EditAuthorizedKeysViewModel>
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
            this.RaiseAndSetIfChanged(ref _selectedKey, value);
            UpdateAddButton();
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
    public ReactiveCommand<ISshKey, ISshKey?> AddKey { get; private set; }

    private void UpdateAddButton()
    {
        if (SelectedKey is null) return;
        AddButtonEnabled = !AuthorizedKeysFileRemote.AuthorizedKeys.Any(key =>
            string.Equals(key.Fingerprint, SelectedKey.ExportAuthorizedKey().Fingerprint));
    }

    public void SetConnectionAndKeys(ref IServerConnection serverConnection,
        ref ObservableCollection<ISshKey?> keys)
    {
        _serverConnection = serverConnection;
        AuthorizedKeysFileRemote = ServerConnection.GetAuthorizedKeysFromServer();
        _publicKeys = keys;
        _selectedKey = PublicKeys.FirstOrDefault();
        UpdateAddButton();
        BooleanSubmit = ReactiveCommand.Create<bool, EditAuthorizedKeysViewModel?>(e =>
        {
            if (!e) return this;
            AuthorizedKeysFileLocal.PersistChangesInFile();
            ServerConnection.WriteAuthorizedKeysChangesToServer(AuthorizedKeysFileRemote);
            return this;
        });
        AddKey = ReactiveCommand.CreateFromTask<ISshKey, ISshKey?>(async e =>
        {
            await AuthorizedKeysFileRemote.AddAuthorizedKeyAsync(e);
            UpdateAddButton();
            return e;
        });
    }
}