using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.ViewModels;
using OpenSSH_GUI.Views;

namespace OpenSSH_GUI;

public class App : Application
{
    internal static IServiceProvider ServiceProvider { get; set; } = null!;

    public override void Initialize() 
        => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = ServiceProvider.ResolveView<MainWindow, MainWindowViewModel>();
        base.OnFrameworkInitializationCompleted();
    }
}