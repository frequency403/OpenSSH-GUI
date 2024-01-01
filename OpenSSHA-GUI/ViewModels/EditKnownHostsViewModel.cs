using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.ReactiveUI;
using OpenSSHALib.Lib;
using OpenSSHALib.Model;
using ReactiveUI;

namespace OpenSSHA_GUI.ViewModels;

public class EditKnownHostsViewModel : ViewModelBase
{
    
    public EditKnownHostsViewModel()
    {
        var hostsFile = new KnownHostsFile(Settings.KnownHostsFilePath);

        hostsFile.ReadContent();
        
        KnownHosts = new ObservableCollection<KnownHosts>(hostsFile.KnownHosts.OrderBy(e => e.Host));
        
        ProcessData = ReactiveCommand.Create<string, EditKnownHostsViewModel>(e =>
        {
            if (!bool.Parse(e)) return this;
            hostsFile.SyncKnownHosts(KnownHosts);
            hostsFile.UpdateFile();
            return this;
        });
    }
    
    public ObservableCollection<KnownHosts> KnownHosts { get; }
    public ReactiveCommand<string, EditKnownHostsViewModel> ProcessData { get; }
    
}