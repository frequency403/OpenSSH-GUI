#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:38

#endregion

using System;
using Avalonia.ReactiveUI;
using OpenSSH_GUI.ViewModels;
using ReactiveUI;

namespace OpenSSH_GUI.Views;

public partial class ConnectToServerWindow : ReactiveWindow<ConnectToServerViewModel>
{
    public ConnectToServerWindow()
    {
        InitializeComponent();
        this.WhenActivated(d => d(ViewModel!.SubmitConnection.Subscribe(Close)));
    }
}