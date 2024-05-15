#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:39

#endregion

using System;
using Avalonia.ReactiveUI;
using OpenSSH_GUI.ViewModels;
using ReactiveUI;

namespace OpenSSH_GUI.Views;

public partial class EditSavedServerEntry : ReactiveWindow<EditSavedServerEntryViewModel>
{
    public EditSavedServerEntry()
    {
        InitializeComponent();
        this.WhenActivated(d => d(ViewModel!.Close.Subscribe(Close)));
    }
}