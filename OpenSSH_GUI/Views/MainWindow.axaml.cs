using Avalonia.Controls;
using Avalonia.Media.Imaging;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Interfaces.Hosts;
using OpenSSH_GUI.Core.MVVM;
using OpenSSH_GUI.Core.Resources.Wrapper;
using OpenSSH_GUI.ViewModels;
using ReactiveUI;

namespace OpenSSH_GUI.Views;

[UsedImplicitly]
public partial class MainWindow : WindowBase<MainWindowViewModel>, IDialogHost
{
    public MainWindow()
    {
        InitializeComponent();
    }
    
    public Task ShowDialog<TWindow>(TWindow dialogWindow) where TWindow : Window
    {
        Logger.LogDebug("Showing dialog {nameOfWindow}", typeof(TWindow).Name);
        return dialogWindow.ShowDialog(this);
    }

    public
#if DEBUG
        async
#endif
        Task<TResult?> ShowDialog<TWindow, TResult>(TWindow dialogWindow)
        where TWindow : Window where TResult : ViewModelBase
    {
        Logger.LogDebug("Showing dialog {nameOfWindow} with expected result {nameOfResult}", typeof(TWindow).Name,
            typeof(TResult).Name);
#if !DEBUG
        return dialogWindow.ShowDialog<TResult?>(this);
#else
        var result = await dialogWindow.ShowDialog<TResult?>(this);
        Logger.LogDebug("Result: {nameOfResult}", result?.GetType().Name);
        return result;
#endif
    }
}