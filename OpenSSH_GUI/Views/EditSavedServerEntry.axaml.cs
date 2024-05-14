// File Created by: Oliver Schantz
// Created: 14.05.2024 - 13:05:41
// Last edit: 14.05.2024 - 13:05:41

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