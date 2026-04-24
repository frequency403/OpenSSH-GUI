using Avalonia.Controls;

namespace OpenSSH_GUI.Core.Interfaces.Hosts;

public interface IDialogHost
{
    public Task ShowDialog<TWindow>(TWindow dialogWindow) where TWindow : Window;
}