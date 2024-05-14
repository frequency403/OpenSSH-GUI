#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 14.05.2024 - 00:05:30
// Last edit: 14.05.2024 - 03:05:30

#endregion

using System;
using Avalonia.ReactiveUI;
using OpenSSH_GUI.ViewModels;
using ReactiveUI;

namespace OpenSSH_GUI.Views;

public partial class ApplicationSettingsWindow : ReactiveWindow<ApplicationSettingsViewModel>
{
    public ApplicationSettingsWindow()
    {
        InitializeComponent();
        this.WhenActivated(d => d(ViewModel!.Submit.Subscribe(Close)));
    }
}