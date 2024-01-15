OpenSSHA-GUI

A GUI for managing your SSH Keys

### Installing

No Installation needed! Just run the OpenSSHA_GUI.exe or .bin 

## Usage

It is free to you, if you connect to a Server or not.  
This program can be used on PC's (Local Machines) and Servers!

If you choose to connect to a server - ***beware!***  
This program - nor the author(s) take responsibility for saved messed up files!  
***Make a backup if you already have files!***

If you need help, open an [Issue]()

The program has a tooltip on every icon, describing what will happen  
if you click on it.

#### Main Window
![](images/MainWindow.png)

#### Add SSH Key
![](images/AddKeyWindow.png)

#### Connect to a Server
Right-Click on the Connection-status icon and click "Connect" on the showing menu.

![](images/ConnectToServerWindow.png)

- You can also auth with a public key from the recognized keys on your machine!   
![](images/ConnectToServerWindowWithKey.png)

- You need to test the connection before you can submit it.  
If you get a connection error, an error window shows up.  
![](images/ConnectToServerWindowSuccess.png)

#### Edit Authorized Keys

Edit your local (or remote) authorized_keys!

![](images/EditAuthorizedKeysWindow.png)

In the remote Version you can even add a key from the recognized keys!
The key cannot be added, when it's already present on the remote!
![](images/EditAuthorizedKeysWindowRemote.png)

#### Edit Known Hosts Window
![](images/KnownHostsWindow.png)

Here you have a list of all "Known Hosts" from your "known_hosts" file.
If you want to remove one key from a Host, toggle the button of the specific Key.
If you want to remove the whole host, just toggle the button on the top label.

#### Export Key Window
![](images/ExportKeyWindow.png)

#### Tooltips

***Tooltip when not connected to a server***   
![](images/tooltip.png)

***Tooltip from Key***   
![](images/tooltipKey.png)

***Tooltip from connection***   
![](images/tooltipServer.png)

## Plans for the future

- [X] ~~Add functionality for putting a key onto a Server~~
- [ ] Beautify UI
- [X] ~~Add functionality for editing authorized_keys~~
- [ ] Add functionality for editing local and remote SSH (user/root) Settings
- [ ] Add functionality for editing application settings
- [ ] Servers should be saved and quickly accessed in the connect window.
- many more not yet known!

## Authors

  - **Oliver Schantz** - *Idea and primary development* -
    [GitHub](https://github.com/frequency403)

See also the list of
[contributors](https://github.com/frequency403/OpenSSH-GUI/contributors)
who participated in this project.

## License

This project is licensed under the [GNU General Public License v3](LICENSE)
- see the [LICENSE](LICENSE) file for
details

