// File Created by: Oliver Schantz
// Created: 27.05.2024 - 08:05:50
// Last edit: 27.05.2024 - 08:05:51

using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.MVVM;
using ReactiveUI.Avalonia;

namespace OpenSSH_GUI.Core.Resources.Wrapper;

public class WindowBase<T>(ILogger<WindowBase<T>> logger) : ReactiveWindow<T> where T : ViewModelBase<T>
{
    public void AddBitmap(Bitmap bitmap)
    {
        Icon = new WindowIcon(bitmap);
    }
    
    public void AttachCloseRequest()
    {
        if(ViewModel is null)
            ArgumentNullException.ThrowIfNull(ViewModel);
        ViewModel.RequestCose += RequestClose;
    }

    private void RequestClose(object? sender, EventArgs e)
    {
        logger.LogDebug("RequestClose from {sender}", sender);
        Close();
    }
}