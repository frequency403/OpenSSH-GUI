using Avalonia.Controls;
using OpenSSH_GUI.Core.MVVM;

namespace OpenSSH_GUI.Core.Interfaces.Hosts;

public interface IDialogHost
{
    public Task ShowDialog<TWindow>(TWindow dialogWindow) where TWindow : Window;

    public Task<TResult?> ShowDialogAsync<TWindow, TResult>(TWindow dialogWindow)
        where TWindow : Window where TResult : ViewModelBase;
}