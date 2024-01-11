using System.Reactive;
using OpenSSHALib.Enums;
using OpenSSHALib.Extensions;
using OpenSSHALib.Lib;
using OpenSSHALib.Models;
using ReactiveUI;

namespace OpenSSHA_GUI.ViewModels;

public class EditAuthorizedKeysViewModel : ViewModelBase
{
    public EditAuthorizedKeysViewModel(ServerConnection serverConnection)
    {
        Submit = ReactiveCommand.Create<Unit, EditAuthorizedKeysViewModel>(e => this);
        _serverConnection = serverConnection;
        AuthorizedKeysFileRemote = ServerConnection.GetAuthorizedKeysFromServer();
    }

    private ServerConnection _serverConnection ;
    public ServerConnection ServerConnection
    {
        get => _serverConnection;
        set => this.RaiseAndSetIfChanged(ref _serverConnection, value);
    }
    
    public AuthorizedKeysFile AuthorizedKeysFileLocal { get; set; } = new (SshConfigFiles.Authorized_Keys.GetPathOfFile());
    public AuthorizedKeysFile AuthorizedKeysFileRemote { get; set; } 
    public ReactiveCommand<Unit, EditAuthorizedKeysViewModel> Submit { get; }
}