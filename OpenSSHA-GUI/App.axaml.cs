using System;
using System.Reactive;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using OpenSSHA_GUI.ViewModels;
using OpenSSHA_GUI.Views;
using ReactiveUI;

namespace OpenSSHA_GUI;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel()
            };
        base.OnFrameworkInitializationCompleted();
    }
    
    private void CloseProgram(object? sender, EventArgs e)
    {
        Environment.Exit(0);
    }
}