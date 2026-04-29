using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenSSH_GUI.Core;
using OpenSSH_GUI.Core.Configuration;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Interfaces;
using OpenSSH_GUI.Core.Interfaces.Hosts;
using OpenSSH_GUI.Core.Lib.Keys;
using OpenSSH_GUI.Core.Lib.Misc;
using OpenSSH_GUI.Core.Resources;
using OpenSSH_GUI.Core.Services;
using OpenSSH_GUI.Core.Services.Hosted;
using OpenSSH_GUI.Dialogs.Interfaces;
using OpenSSH_GUI.Dialogs.Services;
using OpenSSH_GUI.ViewModels;
using OpenSSH_GUI.Views;
using Serilog.Core;
#if DEBUG
using Serilog.Events;
#endif

namespace OpenSSH_GUI.Extensions;

public static class DependencyInjectionExtensions
{
    extension(IHostBuilder builder)
    {
        internal IHostBuilder RegisterOpenSshGuiServices()
        {
            builder.ConfigureServices((hostBuilderContext, services) =>
            {
                services.AddSingleton<App>();
                services.AddSingleton<Application>(sp => sp.GetRequiredService<App>());
                services.AddSingleton<AppIconStore>();
                services.AddSingleton<ExceptionHandler>();
                services.AddSingleton<LoggingLevelSwitch>(_ =>
                    new LoggingLevelSwitch(
#if DEBUG
                        LogEventLevel.Verbose
#endif
                    ));

                services.AddWritableConfiguration<ApplicationConfiguration>(
                    hostBuilderContext.Configuration, string.Empty, ApplicationConfiguration.DefaultApplicationConfigurationFileFullPath);

                services.AddSingleton<ServerConnectionService>();
                services.AddSingleton<DirectoryCrawler>();
                services.AddSingleton<IDirectoryCrawler>(sp => sp.GetRequiredService<DirectoryCrawler>());
                services.AddSingleton<ISshKeyFactory, SshKeyFactory>();
                services.AddSingleton<IKeyFileBackupService, KeyFileBackupService>();
                services.AddSingleton<ISshKeyGenerator, SshKeyGenerator>();
                services.AddSingleton<IKeyFileWriterService, KeyFileWriterService>();
                services.AddSingleton<SshKeyManager>();

                services.AddSingleton<Window>(sp => sp.GetRequiredKeyedService<MainWindow>(nameof(MainWindow)));
                services.AddSingleton<IDialogHost>(sp => sp.GetRequiredKeyedService<MainWindow>(nameof(MainWindow)));
                services.AddSingleton<IClipboard>(sp => sp.GetRequiredKeyedService<MainWindow>(nameof(MainWindow)).Clipboard!);
                services.AddSingleton<IStorageProvider>(sp => sp.GetRequiredKeyedService<MainWindow>(nameof(MainWindow)).StorageProvider);
                services.AddSingleton<ILauncher>(sp => sp.GetRequiredKeyedService<MainWindow>(nameof(MainWindow)).Launcher);

                services.RegisterViewWithViewModel<MainWindow, MainWindowViewModel>(ServiceLifetime.Singleton);
                services.RegisterViewWithViewModel<ExportWindow, ExportWindowViewModel>();
                services.RegisterViewWithViewModel<EditKnownHostsWindow, EditKnownHostsWindowViewModel>();
                services.RegisterViewWithViewModel<EditAuthorizedKeysWindow, EditAuthorizedKeysViewModel>();
                services.RegisterViewWithViewModel<ConnectToServerWindow, ConnectToServerViewModel>();
                services.RegisterViewWithViewModel<AddKeyWindow, AddKeyWindowViewModel>();
                services.RegisterViewWithViewModel<ApplicationSettingsWindow, ApplicationSettingsViewModel>();
                services.RegisterViewWithViewModel<FileInfoWindow, FileInfoWindowViewModel>();
                
                services.AddTransient<IMessageBoxProvider, MessageBoxProvider>();
                services.AddTransient<SshKeyFile>();
                services.AddHostedService<FileSystemAnalyzer>();

                services.AddOptionsWithValidateOnStart<ApplicationConfiguration>().Bind(hostBuilderContext.Configuration);
            });
            return builder;
        }
    }
}