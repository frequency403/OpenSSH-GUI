using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Lib.Static;
using OpenSSH_GUI.ViewModels;
using OpenSSH_GUI.Views;

namespace OpenSSH_GUI;

public class App : Application
{
    internal static IServiceProvider ServiceProvider { get; set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var logger = ServiceProvider.GetRequiredService<ILogger<App>>();
        FileOperations.EnsureFilesAndFoldersExist(logger);
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = ServiceProvider.ResolveView<MainWindow, MainWindowViewModel>();
        base.OnFrameworkInitializationCompleted();
    }
}