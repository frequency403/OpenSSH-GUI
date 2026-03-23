using System.Collections.ObjectModel;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Interfaces.KnownHosts;
using OpenSSH_GUI.Core.Lib.KnownHosts;
using OpenSSH_GUI.Core.MVVM;
using OpenSSH_GUI.Core.Services;
using ReactiveUI;

namespace OpenSSH_GUI.ViewModels;

[UsedImplicitly]
public class EditKnownHostsWindowViewModel(
    ILogger<EditKnownHostsWindowViewModel> logger,
    ServerConnectionService serverConnectionService) : ViewModelBase<EditKnownHostsWindowViewModel>(logger)
{
    public ServerConnectionService ServerConnectionService => serverConnectionService;
    private IKnownHostsFile? KnownHostsFileLocal { get; set; }
    private IKnownHostsFile? KnownHostsFileRemote { get; set; }

    public ObservableCollection<IKnownHost> KnownHostsRemote
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    } = [];

    public ObservableCollection<IKnownHost> KnownHostsLocal
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    } = [];

    protected override async Task OnBooleanSubmitAsync(bool inputParameter,
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
            await new KnownHostsFile().InitializeAsync(SshConfigFiles.Known_Hosts.GetPathOfFile(),
                token: cancellationToken);
        if (serverConnectionService.IsConnected)
            KnownHostsFileRemote =
                await serverConnectionService.ServerConnection.GetKnownHostsFromServerAsync(cancellationToken);
        KnownHostsLocal = new ObservableCollection<IKnownHost>(KnownHostsFileLocal.KnownHosts.OrderBy(e => e.Host));
        KnownHostsRemote = serverConnectionService.IsConnected ? new ObservableCollection<IKnownHost>(KnownHostsFileRemote!.KnownHosts.OrderBy(e => e.Host)) : [];
        await base.InitializeAsync(cancellationToken);
    }
}