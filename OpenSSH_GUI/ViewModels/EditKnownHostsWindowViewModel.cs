using System.Collections.ObjectModel;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Interfaces.KnownHosts;
using OpenSSH_GUI.Core.Interfaces.Misc;
using OpenSSH_GUI.Core.Lib.KnownHosts;
using OpenSSH_GUI.Core.MVVM;
using ReactiveUI;

namespace OpenSSH_GUI.ViewModels;

public class EditKnownHostsWindowViewModel
    : ViewModelBase<EditKnownHostsWindowViewModel>
{
    public IServerConnection ServerConnection { get; private set; }
    private IKnownHostsFile KnownHostsFileLocal { get; set; }
    private IKnownHostsFile KnownHostsFileRemote { get; set; }

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

    protected override async ValueTask<EditKnownHostsWindowViewModel?> OnBooleanSubmitAsync(bool inputParameter)
    {
        if (!inputParameter) return this;
        KnownHostsFileLocal.SyncKnownHosts(KnownHostsLocal);
        if (ServerConnection.IsConnected) KnownHostsFileRemote.SyncKnownHosts(KnownHostsRemote);
        await KnownHostsFileLocal.UpdateFileAsync();
        if (ServerConnection.IsConnected) await ServerConnection.WriteKnownHostsToServerAsync(KnownHostsFileRemote);
        return this;
    }

    public override async ValueTask InitializeAsync(IInitializerParameters<EditKnownHostsWindowViewModel>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        if (parameters is not EditKnownHostWindowViewModelInitializerParameters initializerParameters)
            throw new ArgumentException("invalid parameters", nameof(parameters));
        ServerConnection = initializerParameters.ServerConnection;
        KnownHostsFileLocal = await new KnownHostsFile().InitializeAsync(SshConfigFiles.Known_Hosts.GetPathOfFile(), token: cancellationToken);
        KnownHostsFileRemote = await ServerConnection.GetKnownHostsFromServerAsync(cancellationToken);
        KnownHostsLocal = new ObservableCollection<IKnownHost>(KnownHostsFileLocal.KnownHosts.OrderBy(e => e.Host));
        KnownHostsRemote = new ObservableCollection<IKnownHost>(KnownHostsFileRemote.KnownHosts.OrderBy(e => e.Host));
        await base.InitializeAsync(parameters, cancellationToken);
    }
}

public record EditKnownHostWindowViewModelInitializerParameters : IInitializerParameters<EditKnownHostsWindowViewModel>
{
    public IServerConnection ServerConnection { get; set; }
}