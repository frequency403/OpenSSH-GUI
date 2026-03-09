using System.Reflection;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Interfaces;
using OpenSSH_GUI.Core.Lib.Keys;
using OpenSSH_GUI.Core.Lib.Misc;
using OpenSSH_GUI.Core.MVVM.Interfaces;
using OpenSSH_GUI.Core.Services;
using OpenSSH_GUI.Core.Services.Hosted;
using OpenSSH_GUI.SshConfig;
using OpenSSH_GUI.ViewModels;
using OpenSSH_GUI.Views;
using ReactiveUI.Avalonia;
using Serilog;
using Serilog.Core;
#if!DEBUG
using Serilog.Events;
#endif
using LoggerConfiguration = OpenSSH_GUI.Core.Configuration.LoggerConfiguration;

namespace OpenSSH_GUI;

internal sealed class Program
{
    private static string GetHostVersion()
    {
        return Assembly.GetEntryAssembly()
                   ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                   ?.InformationalVersion
               ?? Assembly.GetEntryAssembly()?.GetName().Version?.ToString()
               ?? "0.0.0";
    }
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static async Task Main(string[] args)
    {
        using var mainCancellationTokenSource = new CancellationTokenSource();
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(ConfigureServicesInternal)
            .UseSerilog()
            .ConfigureAppConfiguration(ConfigureAppConfiguration)
            .Build();

        await host.StartAsync(mainCancellationTokenSource.Token);

        BuildAvaloniaApp(host.Services)
            .StartWithClassicDesktopLifetime(args);

        await host.StopAsync(mainCancellationTokenSource.Token);
    }

    private static void ConfigureAppConfiguration(HostBuilderContext builderContext, IConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.AddSshConfig(SshConfigFiles.Config.GetPathOfFile(), optional: true, reloadOnChange: true);
        configurationBuilder.AddSshConfig(SshConfigFiles.Sshd_Config.GetPathOfFile(), optional: true, reloadOnChange: true);
        
        configurationBuilder.AddInMemoryCollection([
            new KeyValuePair<string, string?>("RUNNING_VERSION", GetHostVersion())
        ]);
    }

    private static void ConfigureServicesInternal(HostBuilderContext hostBuilderContext, IServiceCollection collection)
    {
        var loggingLevelSwitch = new LoggingLevelSwitch(
#if!DEBUG
            LogEventLevel.Verbose
#endif
        );
        collection.AddSingleton(loggingLevelSwitch);

        var logConfiguration = LoggerConfiguration.Default;
        if (!Directory.Exists(logConfiguration.LogFilePath))
            Directory.CreateDirectory(logConfiguration.LogFilePath);

        Log.Logger = new Serilog.LoggerConfiguration()
            .Enrich.FromLogContext()
#if DEBUG
            .WriteTo.Console(levelSwitch: loggingLevelSwitch)
#endif
            .WriteTo.File(
                logConfiguration.LogFileFullPath,
                levelSwitch: loggingLevelSwitch,
                rollingInterval: RollingInterval.Day)
            .CreateLogger();
        // AddServices

        collection.AddLogging(e => e.AddSerilog());
        collection.AddKeyedSingleton<Bitmap>("AppIcon",
            (_, _) => new Bitmap(AssetLoader.Open(new Uri("avares://OpenSSH_GUI/Assets/appicon.ico"))));
        collection.AddSingleton<IServerConnectionService, ServerConnectionService>();
        collection.AddTransient<SshKeyFile>();
        collection.AddTransient<DirectoryCrawler>();
        collection.AddTransient<KeyLocatorService>();
        collection.RegisterViewWithViewModel<MainWindow, MainWindowViewModel>(true,
            serviceCollection =>
            {
                serviceCollection.AddSingleton<IClipboardHost>(sp =>
                    sp.GetRequiredKeyedService<MainWindow>(nameof(MainWindow)));
                serviceCollection.AddSingleton<IDialogHost>(sp =>
                    sp.GetRequiredKeyedService<MainWindow>(nameof(MainWindow)));
            });
        collection.RegisterViewWithViewModel<ExportWindow, ExportWindowViewModel>();
        collection.RegisterViewWithViewModel<EditSavedServerEntry, EditSavedServerEntryViewModel>();
        collection.RegisterViewWithViewModel<EditKnownHostsWindow, EditKnownHostsWindowViewModel>();
        collection.RegisterViewWithViewModel<EditAuthorizedKeysWindow, EditAuthorizedKeysViewModel>();
        collection.RegisterViewWithViewModel<ConnectToServerWindow, ConnectToServerViewModel>();
        collection.RegisterViewWithViewModel<AddKeyWindow, AddKeyWindowViewModel>();
        collection.AddTransient<IClipboardService, ClipboardService>();
        collection.AddHostedService<FileSystemAnalyzer>();
        
        collection.AddKeyedSingleton("ssh_config",
            (_, _) => SshConfigFileService.LoadFromFile(
                SshConfigFiles.Config.GetPathOfFile()));

        collection.AddKeyedSingleton("sshd_config",
            (_, _) => SshConfigFileService.LoadFromFile(
                SshConfigFiles.Sshd_Config.GetPathOfFile()));

    }

    // Avalonia configuration, don't remove; also used by visual designer.
    private static AppBuilder BuildAvaloniaApp(IServiceProvider? serviceProvider = null)
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI(builder => { })
            .AfterSetup(_ =>
            {
                if (serviceProvider is not null)
                    App.ServiceProvider = serviceProvider;
            });
    }
}