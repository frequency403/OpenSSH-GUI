#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:45

#endregion

using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Interfaces.KnownHosts;
using OpenSSH_GUI.Core.Interfaces.Misc;
using OpenSSH_GUI.Core.Lib.KnownHosts;
using ReactiveUI;

namespace OpenSSH_GUI.ViewModels;

public class EditKnownHostsViewModel : ViewModelBase<EditKnownHostsViewModel>
{
    private ObservableCollection<IKnownHost> _knownHostsLocal = [];

    private ObservableCollection<IKnownHost> _knownHostsRemote = [];

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

    public ReactiveCommand<bool, EditKnownHostsViewModel> ProcessData { get; private set; }
    
    public void SetServerConnection(ref IServerConnection connection)
    {
        ServerConnection = connection;
        KnownHostsFileLocal = new KnownHostsFile(SshConfigFiles.Known_Hosts.GetPathOfFile());
        KnownHostsFileRemote = ServerConnection.GetKnownHostsFromServer();
        KnownHostsLocal = new ObservableCollection<IKnownHost>(KnownHostsFileLocal.KnownHosts.OrderBy(e => e.Host));
        KnownHostsRemote = new ObservableCollection<IKnownHost>(KnownHostsFileRemote.KnownHosts.OrderBy(e => e.Host));
        ProcessData = ReactiveCommand.CreateFromTask<bool, EditKnownHostsViewModel>(async e =>
        {
            if (!e) return this;
            KnownHostsFileLocal.SyncKnownHosts(KnownHostsLocal);
            if (ServerConnection.IsConnected) KnownHostsFileRemote.SyncKnownHosts(KnownHostsRemote);
            await KnownHostsFileLocal.UpdateFile();
            if (ServerConnection.IsConnected) ServerConnection.WriteKnownHostsToServer(KnownHostsFileRemote);
            return this;
        });
    }
}