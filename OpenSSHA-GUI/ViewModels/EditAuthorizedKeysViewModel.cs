using System.Reactive;
using ReactiveUI;

namespace OpenSSHA_GUI.ViewModels;

public class EditAuthorizedKeysViewModel
{
    public EditAuthorizedKeysViewModel()
    {
        Submit = ReactiveCommand.Create<Unit, EditAuthorizedKeysViewModel>(e => this);
    }
    
    public ReactiveCommand<Unit, EditAuthorizedKeysViewModel> Submit { get; }
}