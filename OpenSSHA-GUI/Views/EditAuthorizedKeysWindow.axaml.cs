#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 14.01.2024 - 10:01:29
// Last edit: 08.05.2024 - 22:05:59

#endregion

using System;
using Avalonia.ReactiveUI;
using OpenSSHA_GUI.ViewModels;
using ReactiveUI;

namespace OpenSSHA_GUI.Views;

public partial class EditAuthorizedKeysWindow : ReactiveWindow<EditAuthorizedKeysViewModel>
{
    public EditAuthorizedKeysWindow()
    {
        InitializeComponent();
        this.WhenActivated(d => d(ViewModel!.Submit.Subscribe(Close)));
    }
}