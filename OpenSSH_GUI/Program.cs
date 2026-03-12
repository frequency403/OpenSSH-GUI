#if!DEBUG
using Serilog.Events;
#endif
using System.Reflection;
using Avalonia;
using Avalonia.Dialogs;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Interfaces.Hosts;
using OpenSSH_GUI.Core.Interfaces.Services;
using OpenSSH_GUI.Core.Lib.Keys;
using OpenSSH_GUI.Core.Lib.Misc;
using OpenSSH_GUI.Core.Services;
using OpenSSH_GUI.Core.Services.Hosted;
using OpenSSH_GUI.SshConfig;
using OpenSSH_GUI.ViewModels;
using OpenSSH_GUI.Views;
using ReactiveUI.Avalonia;
using Serilog;
using Serilog.Core;
using LoggerConfiguration = OpenSSH_GUI.Core.Configuration.LoggerConfiguration;

namespace OpenSSH_GUI;
[UsedImplicitly]
internal sealed class Program
{
    private const SshConfigFiles ConfigFile = SshConfigFiles.Config;
    private const SshConfigFiles SshdConfig = SshConfigFiles.Sshd_Config;

    private static string GetHostVersion()
    {
        return Assembly.GetEntryAssembly()
                   ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                   ?.InformationalVersion
               ?? Assembly.GetEntryAssembly()?.GetName().Version?.ToString()
               ?? "0.0.0";
    }

#pragma warning disable CA1416
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
        var appBuilder = AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .UseReactiveUI(_ => { })
            .AfterSetup(_ => { App.ServiceProvider = host.Services; })
            .UseManagedSystemDialogs();
        appBuilder.StartWithClassicDesktopLifetime(args);

        await host.StopAsync(mainCancellationTokenSource.Token);
    }
#pragma warning restore CA1416

    private static void ConfigureAppConfiguration(HostBuilderContext builderContext,
        IConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.AddSshConfig(SshConfigFiles.Config.GetPathOfFile(), true, true);
        configurationBuilder.AddSshConfig(SshConfigFiles.Sshd_Config.GetPathOfFile(), true, true);

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
        collection.AddSingleton<DirectoryCrawler>();
        collection.AddSingleton<ISshKeyManager, SshKeyManager>();
        collection.RegisterViewWithViewModel<MainWindow, MainWindowViewModel>(true,
            serviceCollection =>
            {
                serviceCollection.AddSingleton<IClipboardHost>(sp =>
                    sp.GetRequiredKeyedService<MainWindow>(nameof(MainWindow)));
                serviceCollection.AddSingleton<IDialogHost>(sp =>
                    sp.GetRequiredKeyedService<MainWindow>(nameof(MainWindow)));
            });
        collection.RegisterViewWithViewModel<ExportWindow, ExportWindowViewModel>();
        collection.RegisterViewWithViewModel<EditKnownHostsWindow, EditKnownHostsWindowViewModel>();
        collection.RegisterViewWithViewModel<EditAuthorizedKeysWindow, EditAuthorizedKeysViewModel>();
        collection.RegisterViewWithViewModel<ConnectToServerWindow, ConnectToServerViewModel>();
        collection.RegisterViewWithViewModel<AddKeyWindow, AddKeyWindowViewModel>();
        collection.AddTransient<IClipboardService, ClipboardService>();
        collection.AddHostedService<FileSystemAnalyzer>();

        collection.AddKeyedSingleton(ConfigFile,
            (_, _) => SshConfigFileService.LoadFromFile(
                ConfigFile.GetPathOfFile()));

        collection.AddKeyedSingleton(SshdConfig,
            (_, _) => SshConfigFileService.LoadFromFile(
                SshdConfig.GetPathOfFile()));
    }
}