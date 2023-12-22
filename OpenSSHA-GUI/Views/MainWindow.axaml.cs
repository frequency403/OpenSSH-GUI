using System.Threading.Tasks;
using Avalonia.ReactiveUI;
using OpenSSHA_GUI.ViewModels;
using OpenSSHALib.Enums;
using OpenSSHALib.Extensions;
using OpenSSHALib.Model;
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

        var f = new KnownHostsFile(@"C:\Users\frequ\.ssh\known_hosts");
        f.ReadContent();
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
}