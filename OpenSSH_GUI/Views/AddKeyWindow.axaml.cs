#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 21.01.2024 - 23:01:53
// Last edit: 14.05.2024 - 03:05:29

#endregion

using System;
using Avalonia.ReactiveUI;
using OpenSSH_GUI.ViewModels;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;

namespace OpenSSH_GUI.Views;

public partial class AddKeyWindow : ReactiveWindow<AddKeyWindowViewModel>
{
    public AddKeyWindow()
    {
        InitializeComponent();
        this.WhenActivated(d =>
        {
            this.BindValidation<AddKeyWindow, AddKeyWindowViewModel, string, string>(ViewModel, model => model.KeyName,
                window => window.KeyFileNameValidation.Text);
            d(ViewModel!.AddKey.Subscribe(Close));
        });
    }
}