using Avalonia.Controls;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Interfaces.Hosts;
using OpenSSH_GUI.Core.Resources.Wrapper;
using OpenSSH_GUI.ViewModels;

namespace OpenSSH_GUI.Views;

[UsedImplicitly]
public partial class MainWindow : WindowBase<MainWindowViewModel>, IDialogHost
{
    private readonly ILogger<MainWindow> _logger;

    public MainWindow(ILogger<MainWindow> logger)
    {
        _logger = logger;
        InitializeComponent();
    }

    public Task ShowDialog<TWindow>(TWindow dialogWindow) where TWindow : Window
    {
        _logger.LogDebug("Showing dialog {nameOfWindow}", typeof(TWindow).Name);
        return dialogWindow.ShowDialog(this);
    }
}