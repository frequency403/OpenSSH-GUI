using System.Reactive;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Interfaces.AuthorizedKeys;
using OpenSSH_GUI.Core.Interfaces.Services;
using OpenSSH_GUI.Core.Lib.AuthorizedKeys;
using OpenSSH_GUI.Core.Lib.Keys;
using OpenSSH_GUI.Core.MVVM;
using ReactiveUI;

namespace OpenSSH_GUI.ViewModels;

public class EditAuthorizedKeysViewModel(
    ILogger<EditAuthorizedKeysViewModel> logger,
    ISshKeyManager sshKeyManager,
    IServerConnectionService serverConnectionService)
    : ViewModelBase<EditAuthorizedKeysViewModel>(logger)
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

    public ISshKeyManager SshKeyManager => sshKeyManager;
    public IServerConnectionService ServerConnectionService => serverConnectionService;

    public bool KeyAddPossible => SshKeyManager.SshKeys.Count > 0;

    public IAuthorizedKeysFile? AuthorizedKeysFileLocal { get; private set; }
    public IAuthorizedKeysFile? AuthorizedKeysFileRemote { get; private set; }
    public ReactiveCommand<SshKeyFile, Unit>? AddKey { get; private set; }

    protected override async Task OnBooleanSubmitAsync(bool inputParameter,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!inputParameter) return;
            ArgumentNullException.ThrowIfNull(AuthorizedKeysFileLocal);
            await AuthorizedKeysFileLocal.PersistChangesInFileAsync(cancellationToken);
            if (serverConnectionService.IsConnected && AuthorizedKeysFileRemote is not null)
                await serverConnectionService.ServerConnection.WriteAuthorizedKeysChangesToServerAsync(
                    AuthorizedKeysFileRemote, cancellationToken);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error while editing authorized keys");
        }
    }

    public override async ValueTask InitializeAsync(
        IInitializerParameters<EditAuthorizedKeysViewModel>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        AuthorizedKeysFileLocal =
            await AuthorizedKeysFile.OpenAsync(SshConfigFiles.Authorized_Keys.GetPathOfFile(), cancellationToken);
        if (serverConnectionService.IsConnected)
            AuthorizedKeysFileRemote =
                await serverConnectionService.ServerConnection.GetAuthorizedKeysFromServerAsync(cancellationToken);

        _selectedKey = SshKeyManager.SshKeys.FirstOrDefault();
        UpdateAddButton();
        AddKey = ReactiveCommand.CreateFromTask<SshKeyFile>(OnAddKey);
        await base.InitializeAsync(parameters, cancellationToken);
    }

    private async Task OnAddKey(SshKeyFile key)
    {
        if (AuthorizedKeysFileRemote is null) return;
        await AuthorizedKeysFileRemote.AddAuthorizedKeyAsync(key);
        UpdateAddButton();
    }

    private void UpdateAddButton()
    {
        if (SelectedKey is null) return;
        if (AuthorizedKeysFileRemote is null) return;
        AddButtonEnabled = !AuthorizedKeysFileRemote.AuthorizedKeys.Any(key =>
            string.Equals(key.Fingerprint, SelectedKey.Fingerprint));
    }
}