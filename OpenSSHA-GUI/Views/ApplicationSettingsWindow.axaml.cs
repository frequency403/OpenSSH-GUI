// File Created by: Oliver Schantz
// Created: 13.05.2024 - 13:05:20
// Last edit: 13.05.2024 - 13:05:20

using System;
using Avalonia.ReactiveUI;
using OpenSSHA_GUI.ViewModels;
using ReactiveUI;

namespace OpenSSHA_GUI.Views;

public partial class ApplicationSettingsWindow : ReactiveWindow<ApplicationSettingsViewModel>
{
    public ApplicationSettingsWindow()
    {
        InitializeComponent();
        this.WhenActivated(d => d(ViewModel!.Submit.Subscribe(Close)));
    }
}