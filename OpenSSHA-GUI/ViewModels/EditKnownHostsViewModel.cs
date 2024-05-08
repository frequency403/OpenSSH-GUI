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

public class EditKnownHostsViewModel(ILogger<EditKnownHostsViewModel> logger) : ViewModelBase(logger)
{
    private ObservableCollection<IKnownHost> _knownHostsLocal = [];

    private ObservableCollection<IKnownHost> _knownHostsRemote = [];

    public void SetServerConnection(ref IServerConnection connection)
    {
        ServerConnection = connection;
        KnownHostsFileLocal = new KnownHostsFile(SshConfigFiles.Known_Hosts.GetPathOfFile());
        KnownHostsFileRemote = ServerConnection.GetKnownHostsFromServer();
        KnownHostsLocal = new ObservableCollection<IKnownHost>(KnownHostsFileLocal.KnownHosts.OrderBy(e => e.Host));
        KnownHostsRemote = new ObservableCollection<IKnownHost>(KnownHostsFileRemote.KnownHosts.OrderBy(e => e.Host));
        ProcessData = ReactiveCommand.CreateFromTask<string, EditKnownHostsViewModel>(async e =>
        {
            if (!bool.Parse(e)) return this;
            KnownHostsFileLocal.SyncKnownHosts(KnownHostsLocal);
            if (ServerConnection.IsConnected) KnownHostsFileRemote.SyncKnownHosts(KnownHostsRemote);
            await KnownHostsFileLocal.UpdateFile();
            if (ServerConnection.IsConnected) ServerConnection.WriteKnownHostsToServer(KnownHostsFileRemote);
            return this;
        });
    }

    public IServerConnection ServerConnection { get; private set; }

    private IKnownHostsFile KnownHostsFileLocal { get; set; }
    private IKnownHostsFile KnownHostsFileRemote { get; set; }

    public ObservableCollection<IKnownHost> KnownHostsRemote
    {
        get => _knownHostsRemote;
        private set => this.RaiseAndSetIfChanged(ref _knownHostsRemote, value);
    }

    public ObservableCollection<IKnownHost> KnownHostsLocal
    {
        get => _knownHostsLocal;
        private set => this.RaiseAndSetIfChanged(ref _knownHostsLocal, value);
    }

    public ReactiveCommand<string, EditKnownHostsViewModel> ProcessData { get; private set; }
}