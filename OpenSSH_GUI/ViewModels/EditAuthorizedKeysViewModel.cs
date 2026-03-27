using System.Reactive;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Lib.AuthorizedKeys;
using OpenSSH_GUI.Core.Lib.Keys;
using OpenSSH_GUI.Core.MVVM;
using OpenSSH_GUI.Core.Services;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace OpenSSH_GUI.ViewModels;

[UsedImplicitly]
public partial class EditAuthorizedKeysViewModel : ViewModelBase<EditAuthorizedKeysViewModel>
{
    [ObservableAsProperty]
    private bool _addButtonEnabled;
    
    [ObservableAsProperty]
    private bool _keyAddPossible;
    
    [Reactive]
    private SshKeyFile? _selectedKey;
    
    [Reactive]
    private AuthorizedKeysFile _authorizedKeysFileRemote = AuthorizedKeysFile.Empty;
    
    [Reactive]
    private AuthorizedKeysFile _authorizedKeysFileLocal = AuthorizedKeysFile.Empty;
    
    public EditAuthorizedKeysViewModel(ILogger<EditAuthorizedKeysViewModel> logger,
        SshKeyManager sshKeyManager,
        ServerConnectionService serverConnectionService) : base(logger)
    {
        SshKeyManager = sshKeyManager;
        ServerConnectionService = serverConnectionService;
        SelectedKey = SshKeyManager.SshKeys.FirstOrDefault();
        AddKey = ReactiveCommand.CreateFromTask<SshKeyFile>(OnAddKey);
        
        _addButtonEnabledHelper = this.WhenAnyValue(vm => vm.SelectedKey, vm => vm.AuthorizedKeysFileRemote, vm => vm.KeyAddPossible)
            .DistinctUntilChanged()
            .Select(props =>
            {
                if (props is { Item2: { AuthorizedKeys: { Count: > 0 } } col, Item1: { } keyFile , Item3: true})
                    return col.CanAddKey(keyFile);
                return false;
            }).ToProperty(this, vm => vm.AddButtonEnabled).DisposeWith(Disposables);
        
        _keyAddPossibleHelper = this.WhenAnyValue(vm => vm.SshKeyManager.SshKeys.Count)
            .Select(props => props > 0).ToProperty(this, vm => vm.KeyAddPossible).DisposeWith(Disposables);
    }
    
    public SshKeyManager SshKeyManager { get; }
    public ServerConnectionService ServerConnectionService { get; }
    public ReactiveCommand<SshKeyFile, Unit> AddKey { get; }

    protected override async Task BooleanSubmitAsync(bool inputParameter,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!inputParameter) return;
            ArgumentNullException.ThrowIfNull(AuthorizedKeysFileLocal);
            await AuthorizedKeysFileLocal.PersistChangesInFileAsync(cancellationToken);
            if (ServerConnectionService.IsConnected)
                await ServerConnectionService.ServerConnection.WriteAuthorizedKeysChangesToServerAsync(
                    AuthorizedKeysFileRemote, cancellationToken);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error while editing authorized keys");
        }
    }

    public override async ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        AuthorizedKeysFileLocal =
            await AuthorizedKeysFile.OpenAsync(SshConfigFiles.Authorized_Keys.GetPathOfFile(), cancellationToken);
        if (ServerConnectionService.IsConnected)
            AuthorizedKeysFileRemote =
                await ServerConnectionService.ServerConnection.GetAuthorizedKeysFromServerAsync(cancellationToken);
        await base.InitializeAsync(cancellationToken);
    }

    private async Task OnAddKey(SshKeyFile key)
    {
        await AuthorizedKeysFileRemote.AddAuthorizedKeyAsync(key);
    }
}