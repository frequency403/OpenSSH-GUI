using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Database.Context;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Lib.Keys;
using OpenSSH_GUI.Core.Lib.Misc;
using OpenSSH_GUI.Core.Lib.Settings;
using OpenSSH_GUI.Core.Lib.Static;
using OpenSSH_GUI.Core.MVVM.Interfaces;
using OpenSSH_GUI.Core.Services;
using OpenSSH_GUI.ViewModels;
using OpenSSH_GUI.Views;
using Serilog;
using Serilog.Core;
using LoggerConfiguration = OpenSSH_GUI.Core.Configuration.LoggerConfiguration;

namespace OpenSSH_GUI;

public class App : Application
{
    private static ServiceProvider ServiceProvider { get; set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void RegisterServices()
    {
        var collection = new ServiceCollection();
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
        collection.AddTransient<SshKeyFile>();
        collection.AddTransient<DirectoryCrawler>();
        collection.AddTransient<KeyLocatorService>();
        collection.AddDbContext<OpenSshGuiDbContext>();
        collection.RegisterViewWithViewModel<MainWindow, MainWindowViewModel>(true,
            serviceCollection =>
            {
                serviceCollection.AddSingleton<IDialogHost>(sp =>
                    sp.GetRequiredKeyedService<MainWindow>(nameof(MainWindow)));
            });
        collection.RegisterViewWithViewModel<ExportWindow, ExportWindowViewModel>();
        collection.RegisterViewWithViewModel<EditSavedServerEntry, EditSavedServerEntryViewModel>();
        collection.RegisterViewWithViewModel<EditKnownHostsWindow, EditKnownHostsWindowViewModel>();
        collection.RegisterViewWithViewModel<EditAuthorizedKeysWindow, EditAuthorizedKeysViewModel>();
        collection.RegisterViewWithViewModel<ConnectToServerWindow, ConnectToServerViewModel>();
        collection.RegisterViewWithViewModel<ApplicationSettingsWindow, ApplicationSettingsViewModel>();
        collection.RegisterViewWithViewModel<AddKeyWindow, AddKeyWindowViewModel>();

        ServiceProvider = collection.BuildServiceProvider();
        base.RegisterServices();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var db = ServiceProvider.GetRequiredService<OpenSshGuiDbContext>();
        db.Database.Migrate();
        if (!db.Settings.Any()) db.Settings.Add(new Settings());
        db.SaveChanges();

        InitAndOrPrepareServices();
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = ServiceProvider.ResolveView<MainWindow, MainWindowViewModel>();
        base.OnFrameworkInitializationCompleted();
    }

    private void InitAndOrPrepareServices()
    {
        var logger = ServiceProvider.GetRequiredService<ILogger<App>>();
        FileOperations.EnsureFilesAndFoldersExist(logger);
    }

    private void CloseProgram(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) desktop.Shutdown();
    }
}