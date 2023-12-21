using System.Threading.Tasks;
using Avalonia.Controls;
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
    }

    private async Task DoShowDialogAsync(InteractionContext<ConfirmDialogViewModel, ConfirmDialogViewModel?> interaction)
    {
        var dialog = new ConfirmDialog
        {
            DataContext = interaction.Input
        };

        var result = await dialog.ShowDialog<ConfirmDialogViewModel>(this);
        interaction.SetOutput(result);
    }
}