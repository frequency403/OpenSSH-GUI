using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Interfaces.AuthorizedKeys;
using OpenSSH_GUI.Core.Interfaces.Misc;
using OpenSSH_GUI.Core.Lib.AuthorizedKeys;
using OpenSSH_GUI.Core.Lib.Keys;
using OpenSSH_GUI.Core.MVVM;
using OpenSSH_GUI.Core.Services;
using ReactiveUI;

namespace OpenSSH_GUI.ViewModels;

public class EditAuthorizedKeysViewModel(
    ILogger<EditAuthorizedKeysViewModel> logger,
    KeyLocatorService keyLocatorService) : ViewModelBase<EditAuthorizedKeysViewModel>(logger)
{
    private SshKeyFile? _selectedKey;
    private IServerConnection _serverConnection;

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

    public ObservableCollection<SshKeyFile> PublicKeys { get; set; } // TODO = keyLocatorService.SshKeys;

    public bool KeyAddPossible => PublicKeys.Count > 0;

    public IServerConnection ServerConnection
    {
        get => _serverConnection;
        set => this.RaiseAndSetIfChanged(ref _serverConnection, value);
    }

    public IAuthorizedKeysFile AuthorizedKeysFileLocal { get; } =
        new AuthorizedKeysFile(SshConfigFiles.Authorized_Keys.GetPathOfFile());

    public IAuthorizedKeysFile AuthorizedKeysFileRemote { get; private set; }
    public ReactiveCommand<SshKeyFile, SshKeyFile?> AddKey { get; private set; }

    public override ValueTask InitializeAsync(IInitializerParameters<EditAuthorizedKeysViewModel>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        if (parameters is not EditAuthorizedKeysViewModelInitializeParameters initParams)
            throw new ArgumentException("parameters is not valid", nameof(parameters));
        _serverConnection = initParams.ServerConnection;
        AuthorizedKeysFileRemote = ServerConnection.GetAuthorizedKeysFromServer();
        _selectedKey = PublicKeys.FirstOrDefault();
        UpdateAddButton();
        BooleanSubmit = ReactiveCommand.Create<bool, EditAuthorizedKeysViewModel?>(e =>
        {
            if (!e) return this;
            AuthorizedKeysFileLocal.PersistChangesInFile();
            ServerConnection.WriteAuthorizedKeysChangesToServer(AuthorizedKeysFileRemote);
            return this;
        });
        AddKey = ReactiveCommand.CreateFromTask<SshKeyFile, SshKeyFile?>(async e =>
        {
            await AuthorizedKeysFileRemote.AddAuthorizedKeyAsync(e);
            UpdateAddButton();
            return e;
        });
        return base.InitializeAsync(parameters, cancellationToken);
    }

    private void UpdateAddButton()
    {
        if (SelectedKey is null) return;
        AddButtonEnabled = !AuthorizedKeysFileRemote.AuthorizedKeys.Any(key =>
            string.Equals(key.Fingerprint, SelectedKey.Fingerprint()));
    }
}

public record EditAuthorizedKeysViewModelInitializeParameters : IInitializerParameters<EditAuthorizedKeysViewModel>
{
    public IServerConnection ServerConnection { get; set; }
}