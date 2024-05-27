#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:37

#endregion

using System;
using Avalonia.ReactiveUI;
using DynamicData.Binding;
using OpenSSH_GUI.Resources.Wrapper;
using OpenSSH_GUI.ViewModels;
using ReactiveUI;

namespace OpenSSH_GUI.Views;

public partial class ApplicationSettingsWindow : WindowBase<ApplicationSettingsViewModel>
{
    public ApplicationSettingsWindow()
    {
        InitializeComponent();
        this.WhenActivated(d =>
        {
            d(ViewModel!.ShowEditEntry.RegisterHandler(async interaction =>
            {
                var dialog = new EditSavedServerEntry
                {
                    DataContext = interaction.Input,
                    Title = interaction.Input.CredentialsToEdit.Display
                };
                var result = await dialog.ShowDialog<EditSavedServerEntryViewModel>(this);
                interaction.SetOutput(result);
            }));
        });
    }
}