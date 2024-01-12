using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using OpenSSHALib.Enums;
using OpenSSHALib.Extensions;
using OpenSSHALib.Lib;
using OpenSSHALib.Models;
using ReactiveUI;

namespace OpenSSHA_GUI.ViewModels;

public class EditAuthorizedKeysViewModel : ViewModelBase
{
    public EditAuthorizedKeysViewModel(ref ServerConnection serverConnection, ref ObservableCollection<SshPublicKey> keys)
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
        AddKey = ReactiveCommand.CreateFromTask<SshPublicKey, SshPublicKey?>(async e =>
        {
            await AuthorizedKeysFileRemote.AddAuthorizedKeyAsync(e);
            AddButtonEnabled = !AuthorizedKeysFileRemote.AuthorizedKeys.Any(e => e.Fingerprint == SelectedKey.ExportKey().Split(' ')[1]);
            return e;
        });
    }

    private bool _addButtonEnabled = false;

    public bool AddButtonEnabled
    {
        get => _addButtonEnabled;
        set => this.RaiseAndSetIfChanged(ref _addButtonEnabled, value);
    }
    
    private SshPublicKey? _selectedKey;
    public SshPublicKey? SelectedKey
    {
        get => _selectedKey;
        set
        {
            AddButtonEnabled = !AuthorizedKeysFileRemote.AuthorizedKeys.Any(e => e.Fingerprint == value.ExportKey().Split(' ')[1]);
            this.RaiseAndSetIfChanged(ref _selectedKey, value);
        }
    }

    private ObservableCollection<SshPublicKey> _publicKeys;
    public ObservableCollection<SshPublicKey> PublicKeys
    {
        get => _publicKeys;
        set => this.RaiseAndSetIfChanged(ref _publicKeys, value);
    }

    public bool KeyAddPossible => PublicKeys.Count > 0; 
    
    private ServerConnection _serverConnection ;
    public ServerConnection ServerConnection
    {
        get => _serverConnection;
        set => this.RaiseAndSetIfChanged(ref _serverConnection, value);
    }
    
    public AuthorizedKeysFile AuthorizedKeysFileLocal { get; set; } = new (SshConfigFiles.Authorized_Keys.GetPathOfFile());
    public AuthorizedKeysFile AuthorizedKeysFileRemote { get; set; } 
    public ReactiveCommand<string, EditAuthorizedKeysViewModel> Submit { get; }
    public ReactiveCommand<SshPublicKey, SshPublicKey?> AddKey { get; }
}