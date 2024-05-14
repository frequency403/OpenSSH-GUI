#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 14.05.2024 - 00:05:30
// Last edit: 14.05.2024 - 03:05:33

#endregion

using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using OpenSSH_GUI.Core.Interfaces.Settings;
using OpenSSH_GUI.Core.Lib;
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
        collection.AddTransient<MainWindowViewModel>();
        collection.AddTransient<ExportWindowViewModel>();
        collection.AddTransient<EditKnownHostsViewModel>();
        collection.AddTransient<EditAuthorizedKeysViewModel>();
        collection.AddTransient<ConnectToServerViewModel>();
        collection.AddTransient<AddKeyWindowViewModel>();
        collection.AddTransient<ApplicationSettingsViewModel>();
        collection.AddTransient<EditSavedServerEntryViewModel>();
        
        // return ServiceCollection
        return collection;
    }

    private void CloseProgram(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) desktop.Shutdown();
    }
}