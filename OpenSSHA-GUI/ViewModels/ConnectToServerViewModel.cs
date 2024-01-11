using System;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using OpenSSHALib.Lib;
using ReactiveUI;

namespace OpenSSHA_GUI.ViewModels;

public class ConnectToServerViewModel : ViewModelBase
{
    private ServerConnection _serverConnection = new("123", "123", "123");
    public ServerConnection ServerConnection
    {
        get => _serverConnection;
        set => this.RaiseAndSetIfChanged(ref _serverConnection, value);
    }

    public ConnectToServerViewModel()
    {
        TestConnection = ReactiveCommand.CreateFromTask<Unit, Unit>(async e =>
        {
            var task = Task.Run(async () =>
            {
                try
                {
                    if (!ValidData) throw new ArgumentException("Missing hostname/ip, username or password!");
                    ServerConnection = new ServerConnection(Hostname, Username, Password);
                    if (!ServerConnection.TestAndOpenConnection(out var ecException)) throw ecException;
                    StatusButtonText = "Status: success";
                    StatusButtonToolTip = $"Connected to ssh://{Username}@{Hostname}";
                    StatusButtonBackground = Brushes.Green;
                }
                catch (Exception exception)
                {
                    StatusButtonText = "Status: failed!";
                    StatusButtonToolTip = exception.Message;
                    StatusButtonBackground = Brushes.Red;
                    var messageBox = MessageBoxManager.GetMessageBoxStandard(StringsAndTexts.Error, exception.Message,
                        ButtonEnum.Ok, Icon.Error);
                    await messageBox.ShowAsync();
                }
            });
            TryingToConnect = true;
            await task;
            TryingToConnect = false;

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
            return e;
        });
        SubmitConnection = ReactiveCommand.CreateFromTask<Unit, ConnectToServerViewModel>(async e => this);
    }

    private bool ValidData => Hostname != "" && Username != "" && Password != "";

    public string Hostname { get; set; } = "";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";

    private bool _tryingToConnect = false;

    public bool TryingToConnect
    {
        get => _tryingToConnect;
        set => this.RaiseAndSetIfChanged(ref _tryingToConnect, value);
    }
    
    private string _statusButtonToolTip = "Status not yet tested";

    public string StatusButtonToolTip
    {
        get => _statusButtonToolTip;
        set => this.RaiseAndSetIfChanged(ref _statusButtonToolTip, value);
    }

    private string _statusButtonText = "Status: Unknown";

    public string StatusButtonText
    {
        get => _statusButtonText;
        set => this.RaiseAndSetIfChanged(ref _statusButtonText, value);
    }

    private IBrush _statusButtonBackground = Brushes.Gray;

    public IBrush StatusButtonBackground
    {
        get => _statusButtonBackground;
        set => this.RaiseAndSetIfChanged(ref _statusButtonBackground, value);
    }

    public ReactiveCommand<Unit, ConnectToServerViewModel> SubmitConnection { get; }
    public ReactiveCommand<Unit, Unit> TestConnection { get; }
    public ReactiveCommand<Unit, Unit> ResetCommand { get; }
}