// File Created by: Oliver Schantz
// Created: 21.05.2024 - 11:05:13
// Last edit: 21.05.2024 - 11:05:14

using System;
using Avalonia.ReactiveUI;
using OpenSSH_GUI.ViewModels;
using ReactiveUI;

namespace OpenSSH_GUI.Views;

public partial class ConnectionWindow : ReactiveWindow<ConnectionViewModel>
{
    public ConnectionWindow()
    {
        InitializeComponent();
        this.WhenActivated(d => d(ViewModel!.Submit.Subscribe(Close)));
    }
}