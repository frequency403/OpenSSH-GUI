using System.Reflection;
using Avalonia;
using DryIoc;
using DryIoc.Microsoft.DependencyInjection;
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
using LoggerConfiguration = OpenSSH_GUI.Core.Configuration.LoggerConfiguration;

namespace OpenSSH_GUI;

[UsedImplicitly]
internal sealed class Program
{
    public const SshConfigFiles ConfigFile = SshConfigFiles.Config;
    public const SshConfigFiles SshdConfig = SshConfigFiles.Sshd_Config;
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

    private static Logger? CreateLogger(IContainer container)
    {
        var logConfiguration = LoggerConfiguration.Default;
        if (!Directory.Exists(logConfiguration.LogFilePath))
            Directory.CreateDirectory(logConfiguration.LogFilePath);

        return new Serilog.LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.WithCaller()
            .MinimumLevel.ControlledBy(container.Resolve<LoggingLevelSwitch>())
#if DEBUG
            .WriteTo.ColoredConsole(outputTemplate: logConfiguration.LogOutputTemplate)
#endif
            .WriteTo.File(
                logConfiguration.LogFileFullPath,
                outputTemplate: logConfiguration.LogOutputTemplate,
                rollingInterval: RollingInterval.Day)
            .CreateLogger();
    }

#pragma warning disable CA1416
    [STAThread]
    public static async Task Main(string[] args)
    {
        using var container = new Container(rules =>
        {
            var newRules = rules;
            if (!rules.HasMicrosoftDependencyInjectionRules())
                newRules = rules.WithMicrosoftDependencyInjectionRules();

            return newRules.WithDefaultReuse(Reuse.Singleton)
                .WithTrackingDisposableTransients()
                .WithoutThrowOnRegisteringDisposableTransient()
                .WithDefaultIfAlreadyRegistered(IfAlreadyRegistered.Replace);
        });
        container.ConfigureServicesInternal();
        var factory = new DryIocServiceProviderFactory(container);
        using var mainCancellationTokenSource = new CancellationTokenSource();
        var host = Host.CreateDefaultBuilder(args)
            .UseServiceProviderFactory(factory)
            .ConfigureServices(services => services.RegisterOpenSshGuiServices())
            .UseSerilog(CreateLogger(container), true)
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