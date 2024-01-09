using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using OpenSSHALib.Models;
using ReactiveUI;
using ReactiveUI.Validation.Abstractions;
using ReactiveUI.Validation.Contexts;

namespace OpenSSHA_GUI.ViewModels;

public class UploadToServerViewModel : ViewModelBase, IValidatableViewModel
{
    public UploadToServerViewModel(ObservableCollection<SshPublicKey> keys)
    {
        Keys = keys;
        SelectedPublicKey = Keys.First();
        UploadAction = ReactiveCommand.CreateFromTask<Unit, UploadToServerViewModel?>(
            async e =>
            {
                return this;
            });
    }

    public string Hostname { get; set; } = "";
    public string User { get; set; } = "";
    public string Password { get; set; } = "";
    
    public SshPublicKey SelectedPublicKey { get; }
    public ObservableCollection<SshPublicKey> Keys { get; }
    public ValidationContext ValidationContext { get; } = new();
    public ReactiveCommand<Unit, UploadToServerViewModel> UploadAction { get; }
}