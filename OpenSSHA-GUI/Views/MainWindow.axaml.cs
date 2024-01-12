using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using OpenSSHA_GUI.ViewModels;
using ReactiveUI;

namespace OpenSSHA_GUI.Views;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    private const WindowStartupLocation DefaultWindowStartupLocation = WindowStartupLocation.CenterScreen;

    public MainWindow()
    {
        InitializeComponent();
        this.WhenActivated(action => action(ViewModel!.ShowCreate.RegisterHandler(DoShowAddKeyAsync)));
        this.WhenActivated(action => action(ViewModel!.ShowEditKnownHosts.RegisterHandler(DoShowEditKnownHostsAsync)));
        this.WhenActivated(action => action(ViewModel!.ShowExportWindow.RegisterHandler(DoShowExportWindowAsync)));
        this.WhenActivated(action =>
            action(ViewModel!.ShowEditAuthorizedKeys.RegisterHandler(DoShowEditAuthorizedKeysWindowAsync)));
        this.WhenActivated(action =>
            action(ViewModel!.ShowConnectToServerWindow.RegisterHandler(DoShowConnectToServerWindowAsync)));
    }

    private async Task DoShowConnectToServerWindowAsync(
        InteractionContext<ConnectToServerViewModel, ConnectToServerViewModel?> interaction)
    {
        var dialog = new ConnectToServerWindow
        {
            DataContext = interaction.Input,
            WindowStartupLocation = DefaultWindowStartupLocation
        };
        interaction.SetOutput(await dialog.ShowDialog<ConnectToServerViewModel>(this));
    }
    
    private async Task DoShowEditAuthorizedKeysWindowAsync(
        InteractionContext<EditAuthorizedKeysViewModel, EditAuthorizedKeysViewModel?> interaction)
    {
        var dialog = new EditAuthorizedKeysWindow
        {
            DataContext = interaction.Input,
            WindowStartupLocation = DefaultWindowStartupLocation
        };
        interaction.SetOutput(await dialog.ShowDialog<EditAuthorizedKeysViewModel>(this));
    }
    
    private async Task DoShowExportWindowAsync(
        InteractionContext<ExportWindowViewModel, ExportWindowViewModel?> interaction)
    {
        var dialog = new ExportWindow
        {
            DataContext = interaction.Input,
            WindowStartupLocation = DefaultWindowStartupLocation
        };
        interaction.SetOutput(await dialog.ShowDialog<ExportWindowViewModel>(this));
    }

    private async Task DoShowAddKeyAsync(InteractionContext<AddKeyWindowViewModel, AddKeyWindowViewModel?> interaction)
    {
        var dialog = new AddKeyWindow
        {
            DataContext = interaction.Input,
            WindowStartupLocation = DefaultWindowStartupLocation
        };
        interaction.SetOutput(await dialog.ShowDialog<AddKeyWindowViewModel>(this));
    }

    private async Task DoShowEditKnownHostsAsync(
        InteractionContext<EditKnownHostsViewModel, EditKnownHostsViewModel?> interaction)
    {
        var dialog = new EditKnownHostsWindow
        {
            DataContext = interaction.Input,
            WindowStartupLocation = DefaultWindowStartupLocation
        };
        interaction.SetOutput(await dialog.ShowDialog<EditKnownHostsViewModel>(this));
    }
}