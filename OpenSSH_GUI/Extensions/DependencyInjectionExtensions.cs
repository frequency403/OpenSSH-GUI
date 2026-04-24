using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenSSH_GUI.Core;
using OpenSSH_GUI.Core.Extensions;
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
            builder.ConfigureServices((_, services) =>
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

                services.AddSingleton<ServerConnectionService>();
                services.AddSingleton<DirectoryCrawler>();
                services.AddSingleton<SshKeyManager>();
                
                // MainWindow gets a special treatment, because it has to be created with the resolver
                services.AddKeyedSingleton<MainWindow>(nameof(MainWindow), (sp, _) =>
                {
                    var view = ActivatorUtilities.CreateInstance<MainWindow>(sp);
                    foreach (var requiredProperty in view.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(property => property.CanWrite
                                 && property.GetCustomAttribute<RequiredMemberAttribute>() is not null))
                    {
                        if(sp.GetService(requiredProperty.PropertyType) is { } service)
                        {
                            requiredProperty.SetValue(view, service);
                        }
                    }
                    return view;
                });
                services.AddKeyedSingleton<MainWindowViewModel>(nameof(MainWindowViewModel));
                
                services.AddSingleton<Window>(sp => sp.GetRequiredKeyedService<MainWindow>(nameof(MainWindow)));
                services.AddSingleton<IDialogHost>(sp => sp.GetRequiredKeyedService<MainWindow>(nameof(MainWindow)));
                services.AddSingleton<IClipboard>(sp => sp.GetRequiredKeyedService<MainWindow>(nameof(MainWindow)).Clipboard!);
                services.AddSingleton<IStorageProvider>(sp => sp.GetRequiredKeyedService<MainWindow>(nameof(MainWindow)).StorageProvider);
                services.AddSingleton<ILauncher>(sp => sp.GetRequiredKeyedService<MainWindow>(nameof(MainWindow)).Launcher);
                
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
            });
            return builder;
        }
    }
}