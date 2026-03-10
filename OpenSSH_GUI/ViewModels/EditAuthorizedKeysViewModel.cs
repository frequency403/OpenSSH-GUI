using System.Collections.ObjectModel;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Interfaces;
using OpenSSH_GUI.Core.Interfaces.AuthorizedKeys;
using OpenSSH_GUI.Core.Interfaces.Misc;
using OpenSSH_GUI.Core.Lib.AuthorizedKeys;
using OpenSSH_GUI.Core.Lib.Keys;
using OpenSSH_GUI.Core.MVVM;
using OpenSSH_GUI.Core.Services;
using ReactiveUI;

namespace OpenSSH_GUI.ViewModels;

public class EditAuthorizedKeysViewModel(KeyLocatorService keyLocatorService, IServerConnectionService serverConnectionService)
    : ViewModelBase<EditAuthorizedKeysViewModel>
{
    private SshKeyFile? _selectedKey;

    public bool AddButtonEnabled
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public SshKeyFile? SelectedKey
    {
        get => _selectedKey;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedKey, value);
            UpdateAddButton();
        }
    }
    public KeyLocatorService KeyLocatorService => keyLocatorService;
    public IServerConnectionService ServerConnectionService => serverConnectionService;

    public ObservableCollection<SshKeyFile> PublicKeys { get; set; } = keyLocatorService.SshKeys;

    public bool KeyAddPossible => PublicKeys.Count > 0;
    
    public IAuthorizedKeysFile AuthorizedKeysFileLocal { get; private set; }

    public IAuthorizedKeysFile? AuthorizedKeysFileRemote { get; private set; }
    public ReactiveCommand<SshKeyFile, SshKeyFile?> AddKey { get; private set; }

    protected override async ValueTask<EditAuthorizedKeysViewModel?> OnBooleanSubmitAsync(bool inputParameter)
    {
        try
        {
            if (!inputParameter) return this;
            await AuthorizedKeysFileLocal.PersistChangesInFileAsync();
            await serverConnectionService.ServerConnection.WriteAuthorizedKeysChangesToServerAsync(AuthorizedKeysFileRemote);
            return this;
        }
        catch (Exception)
        {
            return this;
        }
    }

    public override async ValueTask InitializeAsync(IInitializerParameters<EditAuthorizedKeysViewModel>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        AuthorizedKeysFileLocal = await AuthorizedKeysFile.OpenAsync(SshConfigFiles.Authorized_Keys.GetPathOfFile(), cancellationToken: cancellationToken);
        if(serverConnectionService.IsConnected)
            AuthorizedKeysFileRemote = await serverConnectionService.ServerConnection.GetAuthorizedKeysFromServerAsync(cancellationToken);
        
        _selectedKey = KeyLocatorService.SshKeys.FirstOrDefault();
        UpdateAddButton();
        AddKey = ReactiveCommand.CreateFromTask<SshKeyFile, SshKeyFile?>(async e =>
        {
            await AuthorizedKeysFileRemote.AddAuthorizedKeyAsync(e);
            UpdateAddButton();
            return e;
        });
        await base.InitializeAsync(parameters, cancellationToken);
    }

    private void UpdateAddButton()
    {
        if (SelectedKey is null) return;
        AddButtonEnabled = !AuthorizedKeysFileRemote.AuthorizedKeys.Any(key =>
            string.Equals(key.Fingerprint, SelectedKey.Fingerprint()));
    }
}