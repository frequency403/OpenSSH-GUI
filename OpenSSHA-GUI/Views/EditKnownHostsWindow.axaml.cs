#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 03.01.2024 - 00:01:22
// Last edit: 08.05.2024 - 22:05:57

#endregion

using System;
using Avalonia.ReactiveUI;
using OpenSSHA_GUI.ViewModels;
using ReactiveUI;

namespace OpenSSHA_GUI.Views;

public partial class EditKnownHostsWindow : ReactiveWindow<EditKnownHostsViewModel>
{
    public EditKnownHostsWindow()
    {
        InitializeComponent();
        this.WhenActivated(d => d(ViewModel!.ProcessData.Subscribe(Close)));
    }
}