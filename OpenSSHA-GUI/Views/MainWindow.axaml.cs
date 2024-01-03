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
        this.WhenActivated(action =>
            action(ViewModel!.ShowConfirm.RegisterHandler(DoShowDialogAsync)));
        this.WhenActivated(action => action(ViewModel!.ShowCreate.RegisterHandler(DoShowAddKeyAsync)));
        this.WhenActivated(action => action(ViewModel!.ShowEditKnownHosts.RegisterHandler(DoShowEditKnownHostsAsync)));
    }

    private async Task DoShowDialogAsync(
        InteractionContext<ConfirmDialogViewModel, ConfirmDialogViewModel?> interaction)
    {
        var dialog = new ConfirmDialog
        {
            DataContext = interaction.Input
        };

        var result = await dialog.ShowDialog<ConfirmDialogViewModel>(this);
        interaction.SetOutput(result);
    }

    private async Task DoShowAddKeyAsync(InteractionContext<AddKeyWindowViewModel, AddKeyWindowViewModel?> interaction)
    {
        var dialog = new AddKeyWindow
        {
            DataContext = interaction.Input
        };

        var result = await dialog.ShowDialog<AddKeyWindowViewModel>(this);
        interaction.SetOutput(result);
    }

    private async Task DoShowEditKnownHostsAsync(
        InteractionContext<EditKnownHostsViewModel, EditKnownHostsViewModel?> interaction)
    {
        var dialog = new EditKnownHostsWindow
        {
            DataContext = interaction.Input
        };

        var result = await dialog.ShowDialog<EditKnownHostsViewModel>(this);
        interaction.SetOutput(result);
    }
}