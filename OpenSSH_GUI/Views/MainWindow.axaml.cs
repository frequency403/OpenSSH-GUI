#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:40

#endregion

using System.Threading.Tasks;
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
        Icon = App.WindowIcon;
        InitializeComponent();
        this.WhenActivated(action => action(ViewModel!.ShowCreate.RegisterHandler(DoShowAddKeyAsync)));
        this.WhenActivated(action => action(ViewModel!.ShowEditKnownHosts.RegisterHandler(DoShowEditKnownHostsAsync)));
        this.WhenActivated(action => action(ViewModel!.ShowExportWindow.RegisterHandler(DoShowExportWindowAsync)));
        this.WhenActivated(action =>
            action(ViewModel!.ShowEditAuthorizedKeys.RegisterHandler(DoShowEditAuthorizedKeysWindowAsync)));
        this.WhenActivated(action =>
            action(ViewModel!.ShowConnectToServerWindow.RegisterHandler(DoShowConnectToServerWindowAsync)));
        this.WhenActivated(action =>
            action(ViewModel!.ShowAppSettings.RegisterHandler(DoShowApplicationSettingsWindowAsync)));
    }

    private async Task DoShowApplicationSettingsWindowAsync(
        IInteractionContext<ApplicationSettingsViewModel, ApplicationSettingsViewModel?> interaction)
    {
        var dialog = new ApplicationSettingsWindow
        {
            DataContext = interaction.Input,
            WindowStartupLocation = DefaultWindowStartupLocation
        };
        interaction.SetOutput(await dialog.ShowDialog<ApplicationSettingsViewModel>(this));
    }

    private async Task DoShowConnectToServerWindowAsync(
        IInteractionContext<ConnectToServerViewModel, ConnectToServerViewModel?> interaction)
    {
        var dialog = new ConnectToServerWindow
        {
            DataContext = interaction.Input,
            WindowStartupLocation = DefaultWindowStartupLocation
        };
        interaction.SetOutput(await dialog.ShowDialog<ConnectToServerViewModel>(this));
    }

    private async Task DoShowEditAuthorizedKeysWindowAsync(
        IInteractionContext<EditAuthorizedKeysViewModel, EditAuthorizedKeysViewModel?> interaction)
    {
        var dialog = new EditAuthorizedKeysWindow
        {
            DataContext = interaction.Input,
            WindowStartupLocation = DefaultWindowStartupLocation
        };
        interaction.SetOutput(await dialog.ShowDialog<EditAuthorizedKeysViewModel>(this));
    }

    private async Task DoShowExportWindowAsync(
        IInteractionContext<ExportWindowViewModel, ExportWindowViewModel?> interaction)
    {
        var dialog = new ExportWindow
        {
            DataContext = interaction.Input,
            WindowStartupLocation = DefaultWindowStartupLocation
        };
        interaction.SetOutput(await dialog.ShowDialog<ExportWindowViewModel>(this));
    }

    private async Task DoShowAddKeyAsync(IInteractionContext<AddKeyWindowViewModel, AddKeyWindowViewModel?> interaction)
    {
        var dialog = new AddKeyWindow
        {
            DataContext = interaction.Input,
            WindowStartupLocation = DefaultWindowStartupLocation
        };
        interaction.SetOutput(await dialog.ShowDialog<AddKeyWindowViewModel>(this));
    }

    private async Task DoShowEditKnownHostsAsync(
        IInteractionContext<EditKnownHostsViewModel, EditKnownHostsViewModel?> interaction)
    {
        var dialog = new EditKnownHostsWindow
        {
            DataContext = interaction.Input,
            WindowStartupLocation = DefaultWindowStartupLocation
        };
        interaction.SetOutput(await dialog.ShowDialog<EditKnownHostsViewModel>(this));
    }
}