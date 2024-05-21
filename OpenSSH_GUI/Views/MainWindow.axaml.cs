#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:40

#endregion

using Avalonia.ReactiveUI;
using OpenSSH_GUI.Resources.Wrapper;
using OpenSSH_GUI.ViewModels;
using ReactiveUI;

namespace OpenSSH_GUI.Views;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    public MainWindow()
    {
        InitializeComponent();
        this.WhenActivated(action => 
            action(ViewModel!.ShowCreate.RegisterHandler(async interaction =>
                await WindowInteraction.DialogMainWindow<AddKeyWindowViewModel, AddKeyWindow>(interaction, this))));
        this.WhenActivated(action => 
            action(ViewModel!.ShowAppSettings.RegisterHandler(async interaction =>
                await WindowInteraction.DialogMainWindow<ApplicationSettingsViewModel, ApplicationSettingsWindow>(interaction, this))));
        this.WhenActivated(action => 
            action(ViewModel!.ShowEditKnownHosts.RegisterHandler(async interaction =>
                await WindowInteraction.DialogMainWindow<EditKnownHostsViewModel, EditKnownHostsWindow>(interaction, this))));
        this.WhenActivated(action => 
            action(ViewModel!.ShowExportWindow.RegisterHandler(async interaction =>
                await WindowInteraction.DialogMainWindow<ExportWindowViewModel, ExportWindow>(interaction, this))));
        this.WhenActivated(action =>
            action(ViewModel!.ShowEditAuthorizedKeys.RegisterHandler(async interaction => 
                await WindowInteraction.DialogMainWindow<EditAuthorizedKeysViewModel, EditAuthorizedKeysWindow>(interaction, this))));
        this.WhenActivated(action =>
            action(ViewModel!.ShowConnectToServerWindow.RegisterHandler(async interaction =>
                await WindowInteraction.DialogMainWindow<ConnectToServerViewModel, ConnectToServerWindow>(interaction, this))));
        this.WhenActivated(action =>
            action(ViewModel!.ShowConnectionWindow.RegisterHandler(async interaction => 
                await WindowInteraction.DialogMainWindow<ConnectionViewModel, ConnectionWindow>(interaction, this))));
    }
}