using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using DryIoc;
using Microsoft.Extensions.DependencyInjection;
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
    private const string IconUri = "avares://OpenSSH_GUI/Assets/appicon.ico";

    extension(IContainer container)
    {
        internal void ConfigureServicesInternal()
        {
            container.Register<App>();
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
            container.Register<MainWindow>(serviceKey: nameof(MainWindow), made: Made.Of(propertiesAndFields: PropertiesAndFields.Auto));
            container.Register<MainWindowViewModel>(serviceKey: nameof(MainWindowViewModel));

            container.RegisterDelegate<IDialogHost>(resolver =>
                resolver.Resolve<MainWindow>(serviceKey: nameof(MainWindow)));
            container.RegisterDelegate<Window>(resolver =>
                resolver.Resolve<MainWindow>(serviceKey: nameof(MainWindow)));
            container.RegisterDelegate<IClipboard>(resolver =>
                resolver.Resolve<MainWindow>(serviceKey: nameof(MainWindow))!.Clipboard!);
            container.RegisterDelegate<IStorageProvider>(resolver =>
                resolver.Resolve<MainWindow>(serviceKey: nameof(MainWindow)).StorageProvider);
            container.RegisterDelegate<ILauncher>(resolver =>
                resolver.Resolve<MainWindow>(serviceKey: nameof(MainWindow)).Launcher);
            container.RegisterDelegate(_ => new Bitmap(AssetLoader.Open(new Uri(IconUri))),
                serviceKey: Program.IconServiceKey);

            container.RegisterViewWithViewModel<ExportWindow, ExportWindowViewModel>();
            container.RegisterViewWithViewModel<EditKnownHostsWindow, EditKnownHostsWindowViewModel>();
            container.RegisterViewWithViewModel<EditAuthorizedKeysWindow, EditAuthorizedKeysViewModel>();
            container.RegisterViewWithViewModel<ConnectToServerWindow, ConnectToServerViewModel>();
            container.RegisterViewWithViewModel<AddKeyWindow, AddKeyWindowViewModel>();

            container.Register<IMessageBoxProvider, MessageBoxProvider>(Reuse.Transient);
            container.Register<SshKeyFile>(Reuse.Transient);

            // container.RegisterInstance(SshConfigFileService.LoadFromFile(Program.ConfigFile.GetPathOfFile()), serviceKey: Program.ConfigFile);
            // container.RegisterInstance(SshConfigFileService.LoadFromFile(Program.SshdConfig.GetPathOfFile()), serviceKey: Program.SshdConfig);
        }
    }

    extension(IServiceCollection collection)
    {
        internal IServiceCollection RegisterOpenSshGuiServices()
        {
            collection.AddHostedService<FileSystemAnalyzer>();
            return collection;
        }
    }
}