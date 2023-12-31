using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.ReactiveUI;
using OpenSSHALib.Lib;
using OpenSSHALib.Model;
using ReactiveUI;

namespace OpenSSHA_GUI.ViewModels;

public class EditKnownHostsViewModel : ViewModelBase
{
    // TODO!

    public EditKnownHostsViewModel()
    {
        DialogResult = ReactiveCommand.Create<string, EditKnownHostsViewModel>(e =>
        {
            return this;
        });

        var hostsFile = new KnownHostsFile(Settings.KnownHostsFilePath);

        hostsFile.ReadContent();
        
        KnownHosts = new ObservableCollection<KnownHosts>(hostsFile.KnownHosts.OrderBy(e => e.Host));
    }
    
    
    public ObservableCollection<KnownHosts> KnownHosts { get; }
    public ReactiveCommand<string, EditKnownHostsViewModel> DialogResult { get; }
}