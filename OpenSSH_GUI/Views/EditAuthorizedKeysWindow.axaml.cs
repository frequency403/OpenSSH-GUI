#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 14.01.2024 - 10:01:29
// Last edit: 14.05.2024 - 03:05:35

#endregion

using System;
using Avalonia.ReactiveUI;
using OpenSSH_GUI.ViewModels;
using ReactiveUI;

namespace OpenSSH_GUI.Views;

public partial class EditAuthorizedKeysWindow : ReactiveWindow<EditAuthorizedKeysViewModel>
{
    public EditAuthorizedKeysWindow()
    {
        InitializeComponent();
        this.WhenActivated(d => d(ViewModel!.Submit.Subscribe(Close)));
    }
}