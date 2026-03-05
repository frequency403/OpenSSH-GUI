#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:40

#endregion

using Avalonia.Controls;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.ViewModels;
using ReactiveUI;
using ReactiveUI.Avalonia;

namespace OpenSSH_GUI.Views;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    private readonly ILogger<MainWindow> _logger;
    private readonly IServiceProvider _serviceProvider;
    private const WindowStartupLocation DefaultWindowStartupLocation = WindowStartupLocation.CenterScreen;

    public MainWindow(ILogger<MainWindow> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
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
        IInteractionContext<ApplicationSettingsViewModel, ApplicationSettingsViewModel?> interaction) =>
        interaction.SetOutput(await _serviceProvider.ResolveView<ApplicationSettingsWindow>()
            .ShowDialog<ApplicationSettingsViewModel>(this));

    private async Task DoShowConnectToServerWindowAsync(
        IInteractionContext<ConnectToServerViewModel, ConnectToServerViewModel?> interaction) =>
        interaction.SetOutput(await _serviceProvider.ResolveView<ConnectToServerWindow>()
            .ShowDialog<ConnectToServerViewModel>(this));

    private async Task DoShowEditAuthorizedKeysWindowAsync(
        IInteractionContext<EditAuthorizedKeysViewModel, EditAuthorizedKeysViewModel?> interaction) =>
        interaction.SetOutput(await _serviceProvider.ResolveView<EditAuthorizedKeysWindow>()
            .ShowDialog<EditAuthorizedKeysViewModel>(this));

    private async Task DoShowExportWindowAsync(
        IInteractionContext<ExportWindowViewModel, ExportWindowViewModel?> interaction) =>
        interaction.SetOutput(
            await _serviceProvider.ResolveView<ExportWindow>().ShowDialog<ExportWindowViewModel>(this));

    private async Task
        DoShowAddKeyAsync(IInteractionContext<AddKeyWindowViewModel, AddKeyWindowViewModel?> interaction) =>
        interaction.SetOutput(
            await _serviceProvider.ResolveView<AddKeyWindow>().ShowDialog<AddKeyWindowViewModel>(this));

    private async Task DoShowEditKnownHostsAsync(
        IInteractionContext<EditKnownHostsWindowViewModel, EditKnownHostsWindowViewModel?> interaction) =>
        interaction.SetOutput(await _serviceProvider.ResolveView<EditKnownHostsWindow>()
            .ShowDialog<EditKnownHostsWindowViewModel>(this));
}