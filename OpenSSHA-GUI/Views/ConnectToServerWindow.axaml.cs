using System;
using Avalonia.ReactiveUI;
using OpenSSHA_GUI.ViewModels;
using ReactiveUI;

namespace OpenSSHA_GUI.Views;

public partial class ConnectToServerWindow : ReactiveWindow<ConnectToServerViewModel>
{
    public ConnectToServerWindow()
    {
        InitializeComponent();
        this.WhenActivated(d => d(ViewModel!.SubmitConnection.Subscribe(Close)));
    }
}