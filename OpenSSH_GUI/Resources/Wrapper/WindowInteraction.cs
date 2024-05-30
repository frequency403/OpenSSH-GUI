// File Created by: Oliver Schantz
// Created: 21.05.2024 - 11:05:46
// Last edit: 21.05.2024 - 11:05:47

using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.ReactiveUI;
using OpenSSH_GUI.ViewModels;
using OpenSSH_GUI.Views;
using ReactiveUI;

namespace OpenSSH_GUI.Resources.Wrapper;

public static class WindowInteraction
{
    public static async Task DialogMainWindow<TViewModel, TWindow>(
        IInteractionContext<TViewModel, TViewModel?> interaction, MainWindow windowOwner)
        where TViewModel : ViewModelBase<TViewModel>, new()
        where TWindow : ReactiveWindow<TViewModel>, new()
    {
        await DialogAnyWindow<TViewModel, TWindow, MainWindow, MainWindowViewModel>(interaction, windowOwner);
    }

    public static async Task DialogAnyWindow<T, TWindow, TWindowOwner, TWindowViewModel>(
        IInteractionContext<T, T?> interaction,
        TWindowOwner windowOwner)
        where T : ViewModelBase<T>, new()
        where TWindow : ReactiveWindow<T>, new()
        where TWindowViewModel : ViewModelBase<TWindowViewModel>, new()
        where TWindowOwner : ReactiveWindow<TWindowViewModel>

    {
        var dialog = new TWindow
        {
            ViewModel = interaction.Input,
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };
        var result = await dialog.ShowDialog<T?>(windowOwner);
        interaction.SetOutput(result);
    }
}