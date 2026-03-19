using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.MVVM;
using ReactiveUI.Avalonia;

namespace OpenSSH_GUI.Core.Resources.Wrapper;

public abstract class WindowBase<T> : ReactiveWindow<T> where T : ViewModelBase<T>
{
    private readonly ILogger<WindowBase<T>> _logger;

    protected WindowBase(ILogger<WindowBase<T>> logger, Bitmap icon)
    {
        _logger = logger;
        Icon = new WindowIcon(icon);
    }

    public void AttachCloseRequest()
    {
        if (ViewModel is null)
            ArgumentNullException.ThrowIfNull(ViewModel);
        ViewModel.Close += RequestClose;
    }

    private void RequestClose(object? sender, EventArgs e)
    {
        _logger.LogDebug("RequestClose from {sender}", sender);
        Close();
    }
}