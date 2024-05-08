using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Extensions.Logging;
using OpenSSHALib.Enums;
using OpenSSHALib.Extensions;
using OpenSSHALib.Interfaces;
using OpenSSHALib.Lib;
using OpenSSHALib.Models;
using ReactiveUI;

namespace OpenSSHA_GUI.ViewModels;

public class EditAuthorizedKeysViewModel(ILogger<EditAuthorizedKeysViewModel> logger) : ViewModelBase(logger)
{
    private bool _addButtonEnabled;

    private ObservableCollection<ISshPublicKey?> _publicKeys;

    private ISshPublicKey? _selectedKey;

    private IServerConnection _serverConnection;
    
    public void SetConnectionAndKeys(ref IServerConnection serverConnection,
        ref ObservableCollection<ISshPublicKey?> keys)
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
        AddKey = ReactiveCommand.CreateFromTask<ISshPublicKey, ISshPublicKey?>(async e =>
        {
            await AuthorizedKeysFileRemote.AddAuthorizedKeyAsync(e);
            var keyExport = await SelectedKey!.ExportKeyAsync();
            AddButtonEnabled =
                AuthorizedKeysFileRemote.AuthorizedKeys.All(key => key.Fingerprint != keyExport!.Split(' ')[1]);
            return e;
        });
    }

    public bool AddButtonEnabled
    {
        get => _addButtonEnabled;
        set => this.RaiseAndSetIfChanged(ref _addButtonEnabled, value);
    }

    public ISshPublicKey? SelectedKey
    {
        get => _selectedKey;
        set
        {
            AddButtonEnabled =
                AuthorizedKeysFileRemote.AuthorizedKeys.All(e => e.Fingerprint != value!.ExportKey()!.Split(' ')[1]);
            this.RaiseAndSetIfChanged(ref _selectedKey, value);
        }
    }

    public ObservableCollection<ISshPublicKey> PublicKeys
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
    public ReactiveCommand<ISshPublicKey, ISshPublicKey?> AddKey { get; private set; }
}