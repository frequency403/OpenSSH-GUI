using Avalonia.Controls;

namespace OpenSSH_GUI.Core.MVVM.Interfaces;

public interface IDialogHost
{
    public Task ShowDialog<TWindow>(TWindow dialogWindow) where TWindow : Window;

    public Task<TResult?> ShowDialog<TWindow, TResult>(TWindow dialogWindow)
        where TWindow : Window where TResult : ViewModelBase;
}