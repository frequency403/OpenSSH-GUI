using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.MVVM;
using ReactiveUI.Avalonia;

namespace OpenSSH_GUI.Core.Resources.Wrapper;

public abstract class WindowBase<T> : ReactiveWindow<T> where T : ViewModelBase<T>
{
    protected readonly ILogger<WindowBase<T>> Logger;

    protected WindowBase(ILogger<WindowBase<T>> logger, Bitmap icon)
    {
        Logger = logger;
        Icon = new WindowIcon(icon);
    }
}