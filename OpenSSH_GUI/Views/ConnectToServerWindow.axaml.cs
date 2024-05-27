#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:38

#endregion

using System;
using Avalonia.ReactiveUI;
using OpenSSH_GUI.ViewModels;
using ReactiveUI;
using System.Reactive;
using OpenSSH_GUI.Resources.Wrapper;

namespace OpenSSH_GUI.Views;

public partial class ConnectToServerWindow : WindowBase<ConnectToServerViewModel>
{
    public ConnectToServerWindow()
    {
        InitializeComponent();
    }
}