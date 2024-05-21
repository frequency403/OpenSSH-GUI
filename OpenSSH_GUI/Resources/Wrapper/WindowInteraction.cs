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
    public static async Task DialogMainWindow<T, TWindow>(IInteractionContext<T, T?> interaction, MainWindow windowOwner) 
        where TWindow : ReactiveWindow<T>, new()
        where T : ViewModelBase
    {
        await DialogAnyWindow<T, TWindow, MainWindow, MainWindowViewModel>(interaction, windowOwner);
    }

    public static async Task DialogAnyWindow<T, TWindow, TWindowOwner, TWindowViewModel>(IInteractionContext<T, T?> interaction,
        TWindowOwner windowOwner)
        where T : ViewModelBase
        where TWindow : ReactiveWindow<T>, new()
        where TWindowViewModel : ViewModelBase
        where TWindowOwner : ReactiveWindow<TWindowViewModel>
    
    {
        var dialog = new TWindow
        {
            DataContext = interaction.Input,
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };
        interaction.SetOutput(await dialog.ShowDialog<T>(windowOwner));
    }
}