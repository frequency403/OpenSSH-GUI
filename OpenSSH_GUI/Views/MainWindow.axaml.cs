#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:40

#endregion

using Avalonia.Controls;
using Avalonia.ReactiveUI;
using OpenSSH_GUI.ViewModels;
using ReactiveUI;

namespace OpenSSH_GUI.Views;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    private const WindowStartupLocation DefaultWindowStartupLocation = WindowStartupLocation.CenterScreen;

    public MainWindow()
    {
        InitializeComponent();
        this.WhenActivated(action => action(ViewModel!.ShowCreate.RegisterHandler(async interaction =>
        {
            var dialog = new AddKeyWindow
            {
                DataContext = interaction.Input,
                WindowStartupLocation = DefaultWindowStartupLocation
            };
            interaction.SetOutput(await dialog.ShowDialog<AddKeyWindowViewModel>(this));
        })));
        this.WhenActivated(action => action(ViewModel!.ShowAppSettings.RegisterHandler(async interaction =>
        {
            var dialog = new ApplicationSettingsWindow
            {
                DataContext = interaction.Input,
                WindowStartupLocation = DefaultWindowStartupLocation
            };
            interaction.SetOutput(await dialog.ShowDialog<ApplicationSettingsViewModel>(this));
        })));
        this.WhenActivated(action => action(ViewModel!.ShowEditKnownHosts.RegisterHandler(async interaction =>
        {
            var dialog = new EditKnownHostsWindow
            {
                DataContext = interaction.Input,
                WindowStartupLocation = DefaultWindowStartupLocation
            };
            interaction.SetOutput(await dialog.ShowDialog<EditKnownHostsViewModel>(this));
        })));
        this.WhenActivated(action => action(ViewModel!.ShowExportWindow.RegisterHandler(async interaction =>
        {
            var dialog = new ExportWindow
            {
                DataContext = interaction.Input,
                WindowStartupLocation = DefaultWindowStartupLocation
            };
            interaction.SetOutput(await dialog.ShowDialog<ExportWindowViewModel>(this));
        })));
        this.WhenActivated(action =>
            action(ViewModel!.ShowEditAuthorizedKeys.RegisterHandler(async interaction =>
            {
                var dialog = new EditAuthorizedKeysWindow
                {
                    DataContext = interaction.Input,
                    WindowStartupLocation = DefaultWindowStartupLocation
                };
                interaction.SetOutput(await dialog.ShowDialog<EditAuthorizedKeysViewModel>(this));
            })));
        this.WhenActivated(action =>
            action(ViewModel!.ShowConnectToServerWindow.RegisterHandler(async interaction =>
            {
                var dialog = new ConnectToServerWindow
                {
                    DataContext = interaction.Input,
                    WindowStartupLocation = DefaultWindowStartupLocation
                };
                interaction.SetOutput(await dialog.ShowDialog<ConnectToServerViewModel>(this));
            })));
    }
}