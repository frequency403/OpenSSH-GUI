using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using DryIoc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenSSH_GUI.Core;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Interfaces.Hosts;
using OpenSSH_GUI.Core.Lib.Keys;
using OpenSSH_GUI.Core.Lib.Misc;
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
    extension(IContainer container)
    {
        internal void ConfigureServicesInternal()
        {
            container.RegisterMany<App>();
            container.Register<ExceptionHandler>();
            container.RegisterInstance(
                new LoggingLevelSwitch(
#if DEBUG
                    LogEventLevel.Verbose
#endif
                ));

            container.Register<ServerConnectionService>();
            container.Register<DirectoryCrawler>();
            container.Register<SshKeyManager>();
            container.Register<MainWindow>(serviceKey: nameof(MainWindow),
                made: Made.Of(propertiesAndFields: PropertiesAndFields.Auto));
            container.Register<MainWindowViewModel>(serviceKey: nameof(MainWindowViewModel));

            container.RegisterDelegate<IDialogHost>(resolver =>
                resolver.Resolve<MainWindow>(nameof(MainWindow)));
            container.RegisterDelegate<Window>(resolver =>
                resolver.Resolve<MainWindow>(nameof(MainWindow)));
            container.RegisterDelegate<IClipboard>(resolver =>
                resolver.Resolve<MainWindow>(nameof(MainWindow))!.Clipboard!);
            container.RegisterDelegate<IStorageProvider>(resolver =>
                resolver.Resolve<MainWindow>(nameof(MainWindow)).StorageProvider);
            container.RegisterDelegate<ILauncher>(resolver =>
                resolver.Resolve<MainWindow>(nameof(MainWindow)).Launcher);

            container.RegisterViewWithViewModel<ExportWindow, ExportWindowViewModel>();
            container.RegisterViewWithViewModel<EditKnownHostsWindow, EditKnownHostsWindowViewModel>();
            container.RegisterViewWithViewModel<EditAuthorizedKeysWindow, EditAuthorizedKeysViewModel>();
            container.RegisterViewWithViewModel<ConnectToServerWindow, ConnectToServerViewModel>();
            container.RegisterViewWithViewModel<AddKeyWindow, AddKeyWindowViewModel>();
            container.RegisterViewWithViewModel<ApplicationSettingsWindow, ApplicationSettingsViewModel>();
            container.RegisterViewWithViewModel<FileInfoWindow, FileInfoWindowViewModel>();

            container.Register<IMessageBoxProvider, MessageBoxProvider>(Reuse.Transient);
            container.Register<SshKeyFile>(Reuse.Transient);
        }
    }

    extension(IHostBuilder builder)
    {
        internal IHostBuilder RegisterOpenSshGuiServices()
        {
            builder.ConfigureServices((_, services) => { services.AddHostedService<FileSystemAnalyzer>(); });
            return builder;
        }
    }
}