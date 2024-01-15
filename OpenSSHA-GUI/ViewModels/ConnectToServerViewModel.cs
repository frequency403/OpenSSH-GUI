using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Media;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using OpenSSHALib.Lib;
using OpenSSHALib.Models;
using ReactiveUI;

namespace OpenSSHA_GUI.ViewModels;

public class ConnectToServerViewModel : ViewModelBase
{
    private bool _authWithPublicKey;

    private string _hostName = "";
    private string _password = "";

    private SshPublicKey? _selectedPublicKey;
    private ServerConnection _serverConnection = new("123", "123", "123");

    private IBrush _statusButtonBackground = Brushes.Gray;

    private string _statusButtonText = "Status: Unknown";

    private string _statusButtonToolTip = "Status not yet tested";

    private bool _tryingToConnect;

    private bool _uploadButtonEnabled;
    private string _userName = "";

    public ConnectToServerViewModel(ref ObservableCollection<SshPublicKey> currentKeys)
    {
        PublicKeys = currentKeys;
        _selectedPublicKey = PublicKeys.FirstOrDefault();
        UploadButtonEnabled = !TryingToConnect && ServerConnection.IsConnected;
        TestConnection = ReactiveCommand.CreateFromTask<Unit, Unit>(async e =>
        {
            var task = Task.Run(() =>
            {
                try
                {
                    if (!ValidData) throw new ArgumentException("Missing hostname/ip, username or password!");
                    ServerConnection = AuthWithPublicKey
                        ? new ServerConnection(Hostname, Username, SelectedPublicKey!)
                        : new ServerConnection(Hostname, Username, Password);
                    if (!ServerConnection.TestAndOpenConnection(out var ecException)) throw ecException;

                    return true;
                }
                catch (Exception exception)
                {
                    StatusButtonToolTip = exception.Message;
                    return false;
                }
            });
            TryingToConnect = true;
            if (await task)
            {
                StatusButtonText = "Status: success";
                StatusButtonToolTip = $"Connected to ssh://{Username}@{Hostname}";
                StatusButtonBackground = Brushes.Green;
            }
            else
            {
                StatusButtonText = "Status: failed!";
                StatusButtonBackground = Brushes.Red;
            }

            TryingToConnect = false;

            if (ServerConnection.IsConnected)
            {
                UploadButtonEnabled = !TryingToConnect && ServerConnection.IsConnected;
                return e;
            }

            var messageBox = MessageBoxManager.GetMessageBoxStandard(StringsAndTexts.Error, StatusButtonToolTip,
                ButtonEnum.Ok, Icon.Error);
            await messageBox.ShowAsync();

            return e;
        });
        ResetCommand = ReactiveCommand.Create<Unit, Unit>(e =>
        {
            Hostname = "";
            Username = "";
            Password = "";
            StatusButtonText = "Status: unknown";
            StatusButtonToolTip = "Status not yet tested!";
            StatusButtonBackground = Brushes.Gray;
            ServerConnection = new ServerConnection("123", "123", "123");
            UploadButtonEnabled = !TryingToConnect && ServerConnection.IsConnected;
            return e;
        });
        SubmitConnection = ReactiveCommand.CreateFromTask<Unit, ConnectToServerViewModel>(async e =>
        {
            await SettingsFileHandler.AddKnownServerToFileAsync(Hostname, Username);
            return this;
        });
    }

    public ServerConnection ServerConnection
    {
        get => _serverConnection;
        private set => this.RaiseAndSetIfChanged(ref _serverConnection, value);
    }

    public SettingsFile ProgramSettings { get; } = SettingsFileHandler.Settings;

    private bool ValidData => SelectedPublicKey is null
        ? Hostname != "" && Username != "" && Password != ""
        : Hostname != "" && Username != "";

    public bool UploadButtonEnabled
    {
        get => _uploadButtonEnabled;
        set => this.RaiseAndSetIfChanged(ref _uploadButtonEnabled, value);
    }

    public bool AuthWithPublicKey
    {
        get => _authWithPublicKey;
        set => this.RaiseAndSetIfChanged(ref _authWithPublicKey, value);
    }

    public SshPublicKey? SelectedPublicKey
    {
        get => _selectedPublicKey;
        set => this.RaiseAndSetIfChanged(ref _selectedPublicKey, value);
    }

    public ObservableCollection<SshPublicKey> PublicKeys { get; }

    public string Hostname
    {
        get => _hostName;
        set => this.RaiseAndSetIfChanged(ref _hostName, value);
    }

    public string Username
    {
        get => _userName;
        set => this.RaiseAndSetIfChanged(ref _userName, value);
    }

    public string Password
    {
        get => _password;
        set => this.RaiseAndSetIfChanged(ref _password, value);
    }

    public bool TryingToConnect
    {
        get => _tryingToConnect;
        set => this.RaiseAndSetIfChanged(ref _tryingToConnect, value);
    }

    public string StatusButtonToolTip
    {
        get => _statusButtonToolTip;
        set => this.RaiseAndSetIfChanged(ref _statusButtonToolTip, value);
    }

    public string StatusButtonText
    {
        get => _statusButtonText;
        set => this.RaiseAndSetIfChanged(ref _statusButtonText, value);
    }

    public IBrush StatusButtonBackground
    {
        get => _statusButtonBackground;
        set => this.RaiseAndSetIfChanged(ref _statusButtonBackground, value);
    }

    public ReactiveCommand<Unit, ConnectToServerViewModel> SubmitConnection { get; }
    public ReactiveCommand<Unit, Unit> TestConnection { get; }
    public ReactiveCommand<Unit, Unit> ResetCommand { get; }
}