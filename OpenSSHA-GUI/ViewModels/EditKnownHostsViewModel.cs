using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using Avalonia.ReactiveUI;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using OpenSSHALib.Lib;
using OpenSSHALib.Models;
using ReactiveUI;

namespace OpenSSHA_GUI.ViewModels;

public class EditKnownHostsViewModel : ViewModelBase
{
    
    public EditKnownHostsViewModel()
    {
        KnownHostsFile = new KnownHostsFile(Settings.KnownHostsFilePath);
        KnownHosts = new ObservableCollection<KnownHost>(KnownHostsFile.KnownHosts.OrderBy(e => e.Host));
        ProcessData = ReactiveCommand.CreateFromTask<string, EditKnownHostsViewModel>(async e =>
        {
            if (!bool.Parse(e)) return this;
            KnownHostsFile.SyncKnownHosts(KnownHosts);
            await KnownHostsFile.UpdateFile();
            return this;
        });
        DeleteHost = ReactiveCommand.Create<KnownHost, Unit>(e =>
        {
            e.KeysDeletionSwitch();
            return new Unit();
        });
        DeleteKey = ReactiveCommand.Create<KnownHostKey, Unit>(e =>
        {
            e.MarkedForDeletion = true;
            return new Unit();
        });
        ResetChangesAndReload = ReactiveCommand.CreateFromTask<Unit, Unit>(async e =>
        {
            await KnownHostsFile.ReadContentAsync();
            KnownHosts = new ObservableCollection<KnownHost>(KnownHostsFile.KnownHosts);
            return e;
        } );
    }
    private KnownHostsFile KnownHostsFile { get; }

    private ObservableCollection<KnownHost> _knownHosts = [];
    public ObservableCollection<KnownHost> KnownHosts
    {
        get => _knownHosts;
        private set => this.RaiseAndSetIfChanged(ref _knownHosts, value);
    }
    
    public ReactiveCommand<string, EditKnownHostsViewModel> ProcessData { get; }
    public ReactiveCommand<Unit, Unit> ResetChangesAndReload { get; } 
    public ReactiveCommand<KnownHost, Unit> DeleteHost { get; } 
    public ReactiveCommand<KnownHostKey, Unit> DeleteKey { get; }
    
}