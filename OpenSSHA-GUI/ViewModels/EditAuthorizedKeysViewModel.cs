using System.Reactive;
using OpenSSHALib.Enums;
using OpenSSHALib.Extensions;
using OpenSSHALib.Models;
using ReactiveUI;

namespace OpenSSHA_GUI.ViewModels;

public class EditAuthorizedKeysViewModel
{
    public EditAuthorizedKeysViewModel()
    {
        Submit = ReactiveCommand.Create<Unit, EditAuthorizedKeysViewModel>(e => this);
    }

    public AuthorizedKeysFile AuthorizedKeysFile = new (SshConfigFiles.Authorized_Keys.GetPathOfFile());
    public ReactiveCommand<Unit, EditAuthorizedKeysViewModel> Submit { get; }
}