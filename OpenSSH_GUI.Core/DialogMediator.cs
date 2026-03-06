using Avalonia.Controls;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.MVVM;

namespace OpenSSH_GUI.Core;

public interface IDialogHost
{
    public Task ShowDialog<TWindow>(TWindow dialogWindow) where TWindow : Window;

    public Task<TResult?> ShowDialog<TWindow, TResult>(TWindow dialogWindow)
        where TWindow : Window where TResult : ViewModelBase;

}