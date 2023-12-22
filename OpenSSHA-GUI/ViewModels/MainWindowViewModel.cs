using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using OpenSSHA_GUI.Views;
using OpenSSHALib.Lib;
using OpenSSHALib.Model;
using ReactiveUI;

namespace OpenSSHA_GUI.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private ObservableCollection<SshKey> _sshKeys = new(DirectoryCrawler.GetAllKeys());

    public Interaction<ConfirmDialogViewModel, ConfirmDialogViewModel?> ShowConfirm = new();
    public Interaction<AddKeyWindowViewModel, AddKeyWindowViewModel?> ShowCreate = new();
    public Interaction<EditKnownHostsViewModel, EditKnownHostsViewModel?> ShowEditKnownHosts = new();

    public ReactiveCommand<Unit, EditKnownHostsViewModel?> OpenEditKnownHostsWindow =>
        ReactiveCommand.CreateFromTask<Unit, EditKnownHostsViewModel?>(async e =>
        {
            var editKnownHosts = new EditKnownHostsViewModel();
            var result = await ShowEditKnownHosts.Handle(editKnownHosts);
            
            // TODO create object for known hosts, that can handle the deletion of keys on its own
            
            return null;
        });
    
    public ReactiveCommand<Unit, AddKeyWindowViewModel?> OpenCreateKeyWindow =>
        ReactiveCommand.CreateFromTask<Unit, AddKeyWindowViewModel?>(async e =>
        {
            var create = new AddKeyWindowViewModel();
            var result = await ShowCreate.Handle(create);
            if (result == null) return result;
            var newKey = await result.RunKeyGen();
            if(newKey!=null) SshKeys.Add(newKey);
            return result;
        });

    public ReactiveCommand<SshKey, ConfirmDialogViewModel?> DeleteKey =>
        ReactiveCommand.CreateFromTask<SshKey, ConfirmDialogViewModel?>(async u =>
        {
            var confirm = new ConfirmDialogViewModel("Really delete the SSH key?", "Yes", "No");
            var result = await ShowConfirm.Handle(confirm);
            if (!result.Consent) return result;
            u.DeleteKeys();
            SshKeys.Remove(u);
            return result;
        });

    public ObservableCollection<SshKey> SshKeys
    {
        get => _sshKeys;
        set => this.RaiseAndSetIfChanged(ref _sshKeys, value);
    }


    public async Task OpenExportWindow(SshKey key)
    {
        var export = await key.ExportKey();
        if (export is null) return;
        var win = new ExportWindow
        {
            DataContext = new ExportWindowViewModel
            {
                Export = export
            },
            Title = $"Export {key.Fingerprint}",
            ShowActivated = true,
            ShowInTaskbar = true,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        win.Show();
    }
}