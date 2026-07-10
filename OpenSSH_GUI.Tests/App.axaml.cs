using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Headless;
using Avalonia.Markup.Xaml;
using OpenSSH_GUI.Tests;
using ReactiveUI.Avalonia;

[assembly: AvaloniaTestApplication(typeof(TestAppBuilder))]
namespace OpenSSH_GUI.Tests;

public partial class App : Application
{
    public override void Initialize() { AvaloniaXamlLoader.Load(this); }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new Window();
        }

        base.OnFrameworkInitializationCompleted();
    }
}



public class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>()
        .UseSkia()
        .UsePlatformDetect()
        .UseReactiveUI(builder => {})
        .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}