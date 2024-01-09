using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using Avalonia.Media;
using OpenSSHALib.Lib;
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
        TestConnection  = ReactiveCommand.CreateFromTask<Unit, Unit>(async e =>
        {
            if (ServerCommunicator.TestConnection(Hostname, User, Password, out var message))
            {
                StatusButtonBackground = Brushes.LimeGreen;
                StatusButtonText = "Status: success";
                StatusButtonToolTip = $"Connection successfully established for ssh://{User}@{Hostname}";
            }
            else
            {
                StatusButtonBackground = Brushes.IndianRed;
                StatusButtonText = "Status: failed";
                StatusButtonToolTip = message;
            }
            return e;
        });
    }

    public string Hostname { get; set; } = "";
    public string User { get; set; } = "";
    public string Password { get; set; } = "";

    public ReactiveCommand<Unit, Unit> TestConnection { get; }
    
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
    
    public SshPublicKey SelectedPublicKey { get; }
    public ObservableCollection<SshPublicKey> Keys { get; }
    public ValidationContext ValidationContext { get; } = new();
    public ReactiveCommand<Unit, UploadToServerViewModel> UploadAction { get; }
}