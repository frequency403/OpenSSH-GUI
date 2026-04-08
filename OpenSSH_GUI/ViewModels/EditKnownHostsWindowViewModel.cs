using System.Collections.ObjectModel;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Lib.KnownHosts;
using OpenSSH_GUI.Core.MVVM;
using OpenSSH_GUI.Core.Services;
using ReactiveUI.SourceGenerators;

namespace OpenSSH_GUI.ViewModels;

[UsedImplicitly]
public partial class EditKnownHostsWindowViewModel(
    ILogger<EditKnownHostsWindowViewModel> logger,
    ServerConnectionService serverConnectionService) : ViewModelBase<EditKnownHostsWindowViewModel>(logger)
{
    [Reactive] private ObservableCollection<KnownHost> _knownHostsLocal = [];

    [Reactive] private ObservableCollection<KnownHost> _knownHostsRemote = [];

    public ServerConnectionService ServerConnectionService => serverConnectionService;
    private KnownHostsFile? KnownHostsFileLocal { get; set; }
    private KnownHostsFile? KnownHostsFileRemote { get; set; }

    protected override async Task BooleanSubmitAsync(bool inputParameter,
        CancellationToken cancellationToken = default)
    {
        if (!inputParameter) return;
        ArgumentNullException.ThrowIfNull(KnownHostsFileLocal);

        KnownHostsFileLocal.SyncKnownHosts(KnownHostsLocal);
        if (serverConnectionService.IsConnected)
            KnownHostsFileRemote?.SyncKnownHosts(KnownHostsRemote);
        await KnownHostsFileLocal.UpdateFileAsync();
        if (!serverConnectionService.IsConnected) return;
        ArgumentNullException.ThrowIfNull(KnownHostsFileRemote);
        await serverConnectionService.ServerConnection.WriteKnownHostsToServerAsync(KnownHostsFileRemote,
            cancellationToken);
    }

    public override async ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        KnownHostsFileLocal =
            await KnownHostsFile.InitializeAsync(new FileInfo(SshConfigFiles.Known_Hosts.GetPathOfFile()),
                token: cancellationToken);
        if (serverConnectionService.IsConnected)
            KnownHostsFileRemote =
                await serverConnectionService.ServerConnection.GetKnownHostsFromServerAsync(cancellationToken);
        KnownHostsLocal = new ObservableCollection<KnownHost>(KnownHostsFileLocal.KnownHosts.OrderBy(e => e.Host));
        KnownHostsRemote = serverConnectionService.IsConnected
            ? new ObservableCollection<KnownHost>(KnownHostsFileRemote!.KnownHosts.OrderBy(e => e.Host))
            : [];
        await base.InitializeAsync(cancellationToken);
    }
}