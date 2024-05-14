#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 14.05.2024 - 00:05:30
// Last edit: 14.05.2024 - 03:05:30

#endregion

using Avalonia.ReactiveUI;
using OpenSSH_GUI.ViewModels;
using ReactiveUI;
using System;

namespace OpenSSH_GUI.Views;

public partial class ApplicationSettingsWindow : ReactiveWindow<ApplicationSettingsViewModel>
{
    public ApplicationSettingsWindow()
    {
        InitializeComponent();
        this.WhenActivated(d =>
        {
            d(ViewModel!.Submit.Subscribe(Close));
            d(ViewModel!.ShowEditEntry.RegisterHandler(async interaction =>
            {
                var dialog = new EditSavedServerEntry
                {
                    DataContext = interaction.Input,
                    Title = $"Edit {interaction.Input.CredentialsToEdit.Display}"
                };
                interaction.SetOutput(await dialog.ShowDialog<EditSavedServerEntryViewModel>(this));
            }));
        });
        
    }
}