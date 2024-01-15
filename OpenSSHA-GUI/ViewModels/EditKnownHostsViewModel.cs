using System.Collections.ObjectModel;
using System.Linq;
using OpenSSHALib.Enums;
using OpenSSHALib.Extensions;
using OpenSSHALib.Lib;
using OpenSSHALib.Models;
using ReactiveUI;

namespace OpenSSHA_GUI.ViewModels;

public class EditKnownHostsViewModel : ViewModelBase
{
    private readonly ObservableCollection<KnownHost> _knownHostsLocal = [];

    private readonly ObservableCollection<KnownHost> _knownHostsRemote = [];
    
    public EditKnownHostsViewModel(ref ServerConnection serverConnection)
    {
        ServerConnection = serverConnection;
        KnownHostsFileLocal = new KnownHostsFile(SshConfigFiles.Known_Hosts.GetPathOfFile());
        KnownHostsFileRemote = ServerConnection.GetKnownHostsFromServer();
        KnownHostsLocal = new ObservableCollection<KnownHost>(KnownHostsFileLocal.KnownHosts.OrderBy(e => e.Host));
        KnownHostsRemote = new ObservableCollection<KnownHost>(KnownHostsFileRemote.KnownHosts.OrderBy(e => e.Host));
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

    public ServerConnection ServerConnection { get; }

    private KnownHostsFile KnownHostsFileLocal { get; }
    private KnownHostsFile KnownHostsFileRemote { get; }

    public ObservableCollection<KnownHost> KnownHostsRemote
    {
        get => _knownHostsRemote;
        private init => this.RaiseAndSetIfChanged(ref _knownHostsRemote, value);
    }

    public ObservableCollection<KnownHost> KnownHostsLocal
    {
        get => _knownHostsLocal;
        private init => this.RaiseAndSetIfChanged(ref _knownHostsLocal, value);
    }

    public ReactiveCommand<string, EditKnownHostsViewModel> ProcessData { get; }
}