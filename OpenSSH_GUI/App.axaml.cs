using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.ViewModels;
using OpenSSH_GUI.Views;

namespace OpenSSH_GUI;

public class App(ILogger<App> logger, IServiceProvider serviceProvider) : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;
        desktop.MainWindow = serviceProvider.GetRequiredKeyedService<MainWindow>("MainWindow");
        logger.LogInformation("MainWindow created");
        desktop.MainWindow.DataContext =
            serviceProvider.GetRequiredKeyedService<MainWindowViewModel>("MainWindowViewModel");
        logger.LogInformation("MainWindowViewModel set");
    }
}