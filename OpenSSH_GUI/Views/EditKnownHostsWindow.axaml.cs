#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 03.01.2024 - 00:01:22
// Last edit: 14.05.2024 - 03:05:30

#endregion

using System;
using Avalonia.ReactiveUI;
using OpenSSH_GUI.ViewModels;
using ReactiveUI;

namespace OpenSSH_GUI.Views;

public partial class EditKnownHostsWindow : ReactiveWindow<EditKnownHostsViewModel>
{
    public EditKnownHostsWindow()
    {
        InitializeComponent();
        this.WhenActivated(d => d(ViewModel!.ProcessData.Subscribe(Close)));
    }
}