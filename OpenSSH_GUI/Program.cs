using System.Reflection;
using Avalonia;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenSSH_GUI.Core;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Extensions;
using OpenSSH_GUI.SshConfig.Extensions;
using ReactiveUI.Avalonia;
using Serilog;
using Serilog.Core;
using Serilog.Sinks.SystemConsole.Themes;
using LoggerConfiguration = OpenSSH_GUI.Core.Configuration.LoggerConfiguration;

namespace OpenSSH_GUI;

// REFACTOR: Change Readme.MD accordingly to new Project functionality;
[UsedImplicitly]
internal sealed class Program
{
    private const SshConfigFiles ConfigFile = SshConfigFiles.Config;
    private const SshConfigFiles SshdConfig = SshConfigFiles.Sshd_Config;
    public const string AppName = "OpenSSH GUI";
    public const string VersionEnvVar = "RUNNING_VERSION";

    private static string GetHostVersion()
    {
        return Assembly.GetEntryAssembly()
                   ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                   ?.InformationalVersion
               ?? Assembly.GetEntryAssembly()?.GetName().Version?.ToString()
               ?? "0.0.0";
    }

    private static void CreateLogger(IServiceProvider serviceProvider, Serilog.LoggerConfiguration loggerConfiguration)
    {
        var logConfiguration = LoggerConfiguration.Default;
        if (!Directory.Exists(logConfiguration.LogFilePath))
            Directory.CreateDirectory(logConfiguration.LogFilePath);

        loggerConfiguration
            .Enrich.FromLogContext()
            .Enrich.WithCaller()
            .MinimumLevel.ControlledBy(serviceProvider.GetRequiredService<LoggingLevelSwitch>())
#if DEBUG
            .WriteTo.Console(outputTemplate: logConfiguration.LogOutputTemplate, theme: AnsiConsoleTheme.Code)
#endif
            .WriteTo.File(
                logConfiguration.LogFileFullPath,
                outputTemplate: logConfiguration.LogOutputTemplate,
                rollingInterval: RollingInterval.Day);
    }

#pragma warning disable CA1416
    [STAThread]
    public static async Task Main(string[] args)
    {
        using var mainCancellationTokenSource = new CancellationTokenSource();
        var host = Host.CreateDefaultBuilder(args)
            .RegisterOpenSshGuiServices()
            .ConfigureLogging((_, builder) => builder.Services.AddSerilog(CreateLogger))
            .ConfigureAppConfiguration(ConfigureAppConfiguration)
            .Build();

        var appBuilder = AppBuilder.Configure(() => host.Services.GetRequiredService<App>())
            .UseSkia()
            .UsePlatformDetect()
            .WithInterFont()
            .UseReactiveUI(configure =>
            {
                configure.WithPlatformServices();
                configure.WithAvalonia();
                configure.WithExceptionHandler(host.Services.GetRequiredService<ExceptionHandler>());
            });

        await host.StartAsync(mainCancellationTokenSource.Token);
        appBuilder.StartWithClassicDesktopLifetime(args);
        await host.StopAsync(mainCancellationTokenSource.Token);
    }
#pragma warning restore CA1416

    private static void ConfigureAppConfiguration(HostBuilderContext builderContext,
        IConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.AddSshConfig(ConfigFile.GetPathOfFile(), true, true, LoggingAction);
        configurationBuilder.AddSshConfig(SshdConfig.GetPathOfFile(), true, true, LoggingAction);
        configurationBuilder.AddInMemoryCollection([
            new KeyValuePair<string, string?>(VersionEnvVar, GetHostVersion())
        ]);
    }

    private static void LoggingAction(string arg1, Exception arg2)
    {
        Log.Logger.Error(arg2, "Failed to load SSH config file: {Path}", arg1);
    }
}