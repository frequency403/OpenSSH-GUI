using System.Threading.Tasks;
using Avalonia.ReactiveUI;
using OpenSSHA_GUI.ViewModels;
using ReactiveUI;

namespace OpenSSHA_GUI.Views;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    public MainWindow()
    {
        InitializeComponent();
        this.WhenActivated(action => action(ViewModel!.ShowCreate.RegisterHandler(DoShowAddKeyAsync)));
        this.WhenActivated(action => action(ViewModel!.ShowEditKnownHosts.RegisterHandler(DoShowEditKnownHostsAsync)));
        this.WhenActivated(action => action(ViewModel!.ShowExportWindow.RegisterHandler(DoShowExportWindowAsync)));
    }

    private async Task DoShowExportWindowAsync(
        InteractionContext<ExportWindowViewModel, ExportWindowViewModel?> interaction)
    {
        var dialog = new ExportWindow
        {
            DataContext = interaction.Input
        };
        interaction.SetOutput(await dialog.ShowDialog<ExportWindowViewModel>(this));
    }

    private async Task DoShowAddKeyAsync(InteractionContext<AddKeyWindowViewModel, AddKeyWindowViewModel?> interaction)
    {
        var dialog = new AddKeyWindow
        {
            DataContext = interaction.Input
        };
        interaction.SetOutput(await dialog.ShowDialog<AddKeyWindowViewModel>(this));
    }

    private async Task DoShowEditKnownHostsAsync(
        InteractionContext<EditKnownHostsViewModel, EditKnownHostsViewModel?> interaction)
    {
        var dialog = new EditKnownHostsWindow
        {
            DataContext = interaction.Input
        };
        interaction.SetOutput(await dialog.ShowDialog<EditKnownHostsViewModel>(this));
    }
}