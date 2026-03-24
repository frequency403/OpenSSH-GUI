using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DryIoc;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Services;
using OpenSSH_GUI.ViewModels;
using OpenSSH_GUI.Views;

namespace OpenSSH_GUI;

public class App(ILogger<App> logger, IResolver resolver) : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        try
        {
            base.OnFrameworkInitializationCompleted();
            if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;
            desktop.MainWindow = await resolver.ResolveViewAsync<MainWindow, MainWindowViewModel>();
            logger.LogInformation("MainWindow created");
            desktop.MainWindow.Opened += OnMainWindowOpened;
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error during application initialization");
        }
    }
    
    /// <summary>
    /// Triggers the initial SSH key search after the main window has been presented,
    /// ensuring the UI is fully ready before background work begins.
    /// </summary>
    private async void OnMainWindowOpened(object? sender, EventArgs e)
    {
        try
        {
            if (sender is Window window)
                window.Opened -= OnMainWindowOpened;

            try
            {
                await resolver.Resolve<SshKeyManager>().InitialSearchAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Initial key search failed");
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error handling MainWindowOpened event");
        }
    }
}