OpenSSH_GUI

A GUI for managing your SSH Keys - on Windows, Linux and macOS!  

The primary reason for creating this project was to give "end-users"  
a modern looking GUI for managing their SSH Keys - and making it easier  
to deploy them to a server of their choice.

The program I found -> [PuSSHy](https://github.com/klimenta/pusshy) was, in my opinion  
not as user-friendly as it could be. I also wanted to use this program on my different  
machines, running on Linux and macOS. So I decided to create my own!   

I hope you like it!

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
##### V2 UI
![](images/NewMainUI.png)  
You can now convert the Key to the opposite format.  
You can choose to delete or keep the key.  
If the key is kept, the program will move it into a newly created sub-folder of your  
.ssh directory.
##### Key without provided password
![](images/FoundPasswordProtectedKey.png)
##### Password options, when a password was provided
![](images/ShowForgetPws.png)
##### Provide password prompt
![](images/ProvidePasswordPrompt.png)
##### Application Settings
![](images/AppSettings.png)
App settings can be accessed through the settings context menu.
There is also an option, that the program converts all PPK keys in your .ssh directory  
to the OpenSSH format. The PPK Keys are not deleted, they will be put into a folder called PPK
![](images/SettingsContextMenu.png)

##### Sorting feature
You can sort the keys, if you want to. Just click on the top description category to sort by.
![](images/Sorted.png)

#### Add SSH Key
![](images/AddKeyWindow.png)

#### Connect to a Server
Right-Click on the Connection-status icon and click "Connect" on the showing menu.

![](images/ConnectToServerWindow.png)

- You can also auth with a public key from the recognized keys on your machine!   
![](images/ConnectToServerWindowWithKey.png)

- V2 Feature: Quick Connect
![](images/ConnectToServerQuickConnect.png)  
If you submitted a valid connection earlier, the program will save the connection,  
and suggest this connection here for quick access.  


- You need to test the connection before you can submit it, if you do not use the new Quick-Connect feature.  
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

## Further Information

- The program will create these at startup without prompting if they don't exist:  
.ssh/(**authorized_keys**, **known_hosts**)  
  (.config/OpenSSH_GUI/ | AppData\Roaming\OpenSSH_GUI\) **OpenSSH_GUI** and a "logs" directory

### Attention: This program will save your Passwords!  
You can not disable this feature. The Passwords are stored when:  
- you enter a server connection with a password
- provide a password for a keyfile

Your passwords are stored on your local machine inside the SQLite Database, protected with AES-Encryption.  
Only the program itself can read any kind of string value inside the database.

## Plans for the future

- [X] ~~Add functionality for putting a key onto a Server~~
- [ ] Beautify UI
- [X] ~~Add functionality for editing authorized_keys~~
- [ ] Add functionality for editing local and remote SSH (user/root) Settings
- [X] ~~Add functionality for editing application settings~~
- [X] ~~Servers should be saved and quickly accessed in the connect window.~~
- many more not yet known!

## Authors

  - **Oliver Schantz** - *Idea and primary development* -
    [GitHub](https://github.com/frequency403)

See also the list of
[contributors](https://github.com/frequency403/OpenSSH-GUI/contributors)
who participated in this project.

## Used Libraries / Technologies

- [Avalonia UI](https://avaloniaui.net/) - Reactive UI

- [ReactiveUI.Validation](https://github.com/reactiveui/ReactiveUI.Validation/)

- [MessageBox.Avalonia](https://github.com/AvaloniaCommunity/MessageBox.Avalonia)

- [Material.Icons](https://github.com/SKProCH/Material.Icons)  

- [SSH.NET](https://github.com/sshnet/SSH.NET)  

- [Serilog](https://serilog.net/)  

- [SshNet.Keygen](https://github.com/darinkes/SshNet.Keygen/)  

- [SshNet.PuttyKeyFile](https://github.com/darinkes/SshNet.PuttyKeyFile)  

- [EntityFrameworkCore](https://github.com/dotnet/EntityFramework.Docs)  

- [EntityFrameworkCore.DataEncryption](https://github.com/Eastrall/EntityFrameworkCore.DataEncryption)

- [SQLite](https://sqlite.org/)

## License

This project is licensed under the [MIT License](LICENSE)
- see the [LICENSE](LICENSE) file for
details

