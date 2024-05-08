#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 21.01.2024 - 23:01:53
// Last edit: 08.05.2024 - 22:05:06

#endregion

using System;
using Avalonia.ReactiveUI;
using OpenSSHA_GUI.ViewModels;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;

namespace OpenSSHA_GUI.Views;

public partial class AddKeyWindow : ReactiveWindow<AddKeyWindowViewModel>
{
    public AddKeyWindow()
    {
        InitializeComponent();
        this.WhenActivated(d =>
        {
            this.BindValidation(ViewModel, model => model.KeyName, window => window.KeyFileNameValidation.Text);
            d(ViewModel!.AddKey.Subscribe(Close));
        });
    }
}