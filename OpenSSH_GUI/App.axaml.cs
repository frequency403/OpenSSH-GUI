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
using OpenSSH_GUI.ViewModels;
using OpenSSH_GUI.Views;
using Serilog;
using Serilog.Events;

namespace OpenSSH_GUI;

public class App : Application
{
    public static ServiceProvider ServiceProvider { get; private set; }
    public static WindowIcon WindowIcon => new (new Bitmap(AssetLoader.Open(new Uri("avares://OpenSSH_GUI/Assets/appicon.ico"))));
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        ServiceProvider = BuildServiceCollection().BuildServiceProvider();
        using (var db = new OpenSshGuiDbContext())
        {
            db.Database.Migrate();
            if (!db.Settings.Any()) db.Settings.Add(new Settings());
            db.SaveChanges();
        }

        InitAndOrPrepareServices();
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel()
            };
        base.OnFrameworkInitializationCompleted();
    }

    private void InitAndOrPrepareServices()
    {
        DirectoryCrawler.ProvideContext(ServiceProvider.GetRequiredService<ILogger<App>>());
        SshConfigFilesExtension.ValidateDirectories();
    }

    private ServiceCollection BuildServiceCollection()
    {
        var collection = new ServiceCollection();
        var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppDomain.CurrentDomain.FriendlyName);
        var logFilebasePath = Path.Combine(appDataPath, "logs");
        if (!Directory.Exists(logFilebasePath)) Directory.CreateDirectory(logFilebasePath);
        var logFilePath = Path.Combine(logFilebasePath, $"{AppDomain.CurrentDomain.FriendlyName}.log");
        var serilog = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.File(logFilePath, LogEventLevel.Debug, rollingInterval: RollingInterval.Day)
            .CreateLogger();
        // AddServices

        collection.AddLogging(e => e.AddSerilog(serilog, true));

        // return ServiceCollection
        return collection;
    }

    private void CloseProgram(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) desktop.Shutdown();
    }
}