#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:35

#endregion

using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Database.Context;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Lib.Misc;
using OpenSSH_GUI.Core.Lib.Settings;
using OpenSSH_GUI.Core.Lib.Static;
using OpenSSH_GUI.ViewModels;
using OpenSSH_GUI.Views;
using Serilog;
using Serilog.Core;

namespace OpenSSH_GUI;

public class App : Application
{
    public static ServiceProvider ServiceProvider { get; private set; }
    public static WindowIcon WindowIcon { get; } = new(new Bitmap(AssetLoader.Open(new Uri("avares://OpenSSH_GUI/Assets/appicon.ico"))));
    
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
        
        var logConfiguration = Core.Configuration.LoggerConfiguration.Default;
        if (!Directory.Exists(logConfiguration.LogFilePath)) 
            Directory.CreateDirectory(logConfiguration.LogFilePath);
        
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            #if DEBUG
            .WriteTo.Console(levelSwitch: loggingLevelSwitch)
            #endif
            .WriteTo.File(
                path: logConfiguration.LogFileFullPath, 
                levelSwitch: loggingLevelSwitch,
                rollingInterval: RollingInterval.Day)
            .CreateLogger();
        // AddServices

        collection.AddLogging(e => e.AddSerilog());
        collection.AddTransient<SshKeyFile>();
        collection.AddTransient<DirectoryCrawler>();
        collection.AddDbContext<OpenSshGuiDbContext>();
        collection.RegisterViewWithViewModel<MainWindow, MainWindowViewModel>();
        
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
            desktop.MainWindow = ServiceProvider.ResolveView<MainWindow>();
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