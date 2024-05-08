using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using OpenSSHA_GUI.ViewModels;
using OpenSSHA_GUI.Views;
using OpenSSHALib.Lib;
using Serilog;
using Serilog.Events;
using System.IO;
using OpenSSHALib.Interfaces;

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
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = ServiceProvider.GetRequiredService<MainWindowViewModel>()
            };
        }
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
       
        collection.AddLogging(e => e.AddSerilog(serilog,dispose: true));
        collection.AddSingleton<IApplicationSettings, ApplicationSettings>();
        collection.AddTransient<DirectoryCrawler>();
        collection.AddTransient<MainWindowViewModel>();
        collection.AddTransient<ExportWindowViewModel>();
        collection.AddTransient<EditKnownHostsViewModel>();
        collection.AddTransient<EditAuthorizedKeysViewModel>();
        collection.AddTransient<ConnectToServerViewModel>();
        collection.AddTransient<AddKeyWindowViewModel>();
        
        // return ServiceCollection
        return collection;
    }
    
    private void CloseProgram(object? sender, EventArgs e)
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) desktop.Shutdown();
    }
}