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
using ReactiveUI.Avalonia;
using ReactiveUI.SourceGenerators;

namespace OpenSSH_GUI.ViewModels;

[UsedImplicitly]
public partial class EditAuthorizedKeysViewModel : ViewModelBase<EditAuthorizedKeysViewModel>
{
    [Reactive(SetModifier = AccessModifier.Private)]
    private bool _addButtonEnabled;

    [Reactive] private AuthorizedKeysFile _authorizedKeysFileLocal = AuthorizedKeysFile.Empty;

    [Reactive] private AuthorizedKeysFile _authorizedKeysFileRemote = AuthorizedKeysFile.Empty;

    [Reactive(SetModifier = AccessModifier.Private)]
    private bool _keyAddPossible;

    [Reactive] private SshKeyFile? _selectedKey;

    public EditAuthorizedKeysViewModel(ILogger<EditAuthorizedKeysViewModel> logger,
        SshKeyManager sshKeyManager,
        ServerConnectionService serverConnectionService) : base(logger)
    {
        SshKeyManager = sshKeyManager;
        ServerConnectionService = serverConnectionService;
        SelectedKey = SshKeyManager.SshKeys.FirstOrDefault();
        AddKey = ReactiveCommand.CreateFromTask<SshKeyFile>(OnAddKey);

        this.WhenAnyValue(vm => vm.SelectedKey, vm => vm.AuthorizedKeysFileRemote, vm => vm.KeyAddPossible)
            .DistinctUntilChanged()
            .Subscribe(props =>
            {
                AddButtonEnabled = props is
                {
                    Item1: { } keyFile,
                    Item2:
                    {
                        AuthorizedKeys:
                        {
                            Count: > 0
                        }
                    } col,
                    Item3: true
                } && col.CanAddKey(keyFile);
            }).DisposeWith(Disposables);

        this.WhenAnyValue(vm => vm.SshKeyManager.SshKeys)
            .ObserveOn(AvaloniaScheduler.Instance)
            .Subscribe(keys => { KeyAddPossible = keys.Count > 0; })
            .DisposeWith(Disposables);
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