using System;
using Avalonia;
using Avalonia.ReactiveUI;
using OpenSSHALib.Lib;

namespace OpenSSHA_GUI;

internal sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        if (!SettingsFileHandler.IsFileInitialized) 
            if (!SettingsFileHandler.InitSettingsFile()) 
                return;
        
        if (!InitializationRoutine.IsProgramStartReady)
            if (!InitializationRoutine.MakeProgramStartReady())
                return;
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .UseReactiveUI();
    }
}