#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 08.05.2024 - 22:05:30
// Last edit: 08.05.2024 - 22:05:02

#endregion

using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using OpenSSHA_GUI.ViewModels;
using OpenSSHA_GUI.Views;
using OpenSSHALib.Interfaces;
using OpenSSHALib.Lib;
using Serilog;
using Serilog.Events;

namespace OpenSSHA_GUI;

public class App : Application
{
    public static ServiceProvider ServiceProvider { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        ServiceProvider = BuildServiceCollection().BuildServiceProvider();
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new MainWindow
            {
                DataContext = ServiceProvider.GetRequiredService<MainWindowViewModel>()
            };
        base.OnFrameworkInitializationCompleted();
    }

    private ServiceCollection BuildServiceCollection()
    {
        var collection = new ServiceCollection();
        var logFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppDomain.CurrentDomain.FriendlyName, "logs", $"{AppDomain.CurrentDomain.FriendlyName}.log");
        var serilog = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.File(logFilePath, LogEventLevel.Debug, rollingInterval: RollingInterval.Day)
            .CreateLogger();
        // AddServices

        collection.AddLogging(e => e.AddSerilog(serilog, true));
        collection.AddSingleton<IApplicationSettings, ApplicationSettings>();
        collection.AddTransient<DirectoryCrawler>();
        collection.AddTransient<MainWindowViewModel>();
        collection.AddTransient<ExportWindowViewModel>();
        collection.AddTransient<EditKnownHostsViewModel>();
        collection.AddTransient<EditAuthorizedKeysViewModel>();
        collection.AddTransient<ConnectToServerViewModel>();
        collection.AddTransient<AddKeyWindowViewModel>();
        collection.AddTransient<ApplicationSettingsViewModel>();
        
        // return ServiceCollection
        return collection;
    }

    private void CloseProgram(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) desktop.Shutdown();
    }
}