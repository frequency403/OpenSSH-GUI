using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Platform.Storage;
using JetBrains.Annotations;
using Material.Icons;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Configuration;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Interfaces;
using OpenSSH_GUI.Core.MVVM;
using OpenSSH_GUI.Dialogs.Enums;
using OpenSSH_GUI.Dialogs.Interfaces;
using OpenSSH_GUI.Dialogs.Models;
using OpenSSH_GUI.Resources;
using ReactiveUI;
using ReactiveUI.Avalonia;
using ReactiveUI.SourceGenerators;
using Serilog.Core;
using Serilog.Events;

namespace OpenSSH_GUI.ViewModels;

[UsedImplicitly]
public partial class ApplicationSettingsViewModel : ViewModelBase
{
    private readonly Application _application;
    private readonly ILauncher _launcher;
    private readonly IStorageProvider _storageProvider;
    private readonly LoggingLevelSwitch _levelSwitch;
    private readonly ILogger<ApplicationSettingsViewModel> _logger;
    private readonly IMutableConfiguration<ApplicationConfiguration> _mutableConfiguration;
    private readonly IMessageBoxProvider _messageBoxProvider;

    [Reactive] private bool _canDeleteOldLogFiles;

    [Reactive] private LogEventLevel _currentLogLevel;

    [Reactive] private ThemeVariant _currentThemeVariant;

    [Reactive] private int _daysToDeleteSelected;

    [Reactive] private double _fontSize;

    [Reactive] private ApplicationConfiguration _applicationConfiguration = ApplicationConfiguration.Default;

    public ApplicationSettingsViewModel(ILogger<ApplicationSettingsViewModel> logger,
        IMutableConfiguration<ApplicationConfiguration> mutableConfiguration,
        ILauncher launcher,
        IStorageProvider storageProvider,
        IMessageBoxProvider messageBoxProvider,
        Application application,
        LoggingLevelSwitch levelSwitch)
    {
        _logger = logger;
        _mutableConfiguration = mutableConfiguration;
        _launcher = launcher;
        _storageProvider = storageProvider;
        _messageBoxProvider = messageBoxProvider;
        _levelSwitch = levelSwitch;
        _currentLogLevel = _mutableConfiguration.Current.LogLevel;
        _application = application;
        _daysToDeleteSelected = DaysToDelete[0];
        _currentThemeVariant = _mutableConfiguration.Current.PreferredTheme;
        _fontSize = _mutableConfiguration.Current.FontSize;

        Observable.FromEventPattern<ApplicationConfiguration>(
                handler => mutableConfiguration.ConfigurationChanged += handler,
                handler => mutableConfiguration.ConfigurationChanged -= handler)
            .ObserveOn(AvaloniaScheduler.Instance)
            .Select(pattern => pattern.EventArgs)
            .Subscribe(c => ApplicationConfiguration = c)
            .DisposeWith(Disposables);

        if (Enum.TryParse<ThemeVariant>(application.ActualThemeVariant.Key.ToString(), true, out var themeVariant))
            _currentThemeVariant = themeVariant;

        Observable
            .FromEventPattern<LoggingLevelSwitchChangedEventArgs>(
                handler => levelSwitch.MinimumLevelChanged += handler,
                handler => levelSwitch.MinimumLevelChanged -= handler
            ).ObserveOn(AvaloniaScheduler.Instance)
            .Select(pattern => Observable.FromAsync(async () =>
            {
                await OnNextLevel(pattern.EventArgs);
                return Unit.Default;
            }))
            .Switch()
            .Subscribe(
                _ => { },
                ex => logger.LogError(ex, "Error while changing loglevel")
            )
            .DisposeWith(Disposables);

        this.WhenAnyValue(model => model.CurrentLogLevel)
            .DistinctUntilChanged()
            .Throttle(TimeSpan.FromMilliseconds(300))
            .ObserveOn(AvaloniaScheduler.Instance)
            .Select(x => Observable.FromAsync(async () =>
            {
                await OnNextLevel(new LoggingLevelSwitchChangedEventArgs(_levelSwitch.MinimumLevel, x));
                return Unit.Default;
            }))
            .Switch()
            .Subscribe(
                _ => { },
                ex => logger.LogError(ex, "Error while changing loglevel")
            )
            .DisposeWith(Disposables);

        this.WhenAnyValue(model => model.DaysToDeleteSelected)
            .ObserveOn(AvaloniaScheduler.Instance)
            .Subscribe(OnNextDaysToDelete)
            .DisposeWith(Disposables);

        this.WhenAnyValue(vm => vm.LogFiles.Count)
            .ObserveOn(AvaloniaScheduler.Instance)
            .DistinctUntilChanged()
            .Subscribe(count => { CanDeleteOldLogFiles = count > 0; })
            .DisposeWith(Disposables);

        this.WhenAnyValue(vm => vm.CurrentThemeVariant)
            .ObserveOn(AvaloniaScheduler.Instance)
            .DistinctUntilChanged()
            .Subscribe(OnNextTheme)
            .DisposeWith(Disposables);

        this.WhenAnyValue(vm => vm.FontSize)
            .DistinctUntilChanged()
            .Throttle(TimeSpan.FromMilliseconds(300))
            .ObserveOn(AvaloniaScheduler.Instance)
            .Select(x => Observable.FromAsync(async () =>
            {
                await OnNextFontSize(x);
                return Unit.Default;
            }))
            .Switch()
            .Subscribe(
                _ => { },
                ex => logger.LogError(ex, "Error while changing font size")
            )
            .DisposeWith(Disposables);
        
        ApplicationConfiguration = mutableConfiguration.Current;
    }

    public static LogEventLevel[] AvailableLogLevels { get; } = Enum.GetValues<LogEventLevel>();
    public static ThemeVariant[] ThemeVariants { get; } = Enum.GetValues<ThemeVariant>();
    public static int[] DaysToDelete { get; } = Enumerable.Range(1, 4).Select(i => i * 7).ToArray();

    public ObservableCollection<string> LogFiles { get; } = [];
    

    [ReactiveCommand]
    private Task DeleteLookupPathAsync(string path, CancellationToken cancellationToken = default) => 
        _mutableConfiguration.SetPropertyValueAsync(conf => conf.LookupPaths, _mutableConfiguration.Current.LookupPaths.Where(p => p != path).ToArray(), cancellationToken);

    [ReactiveCommand]
    private async Task AddLookupPathAsync(CancellationToken cancellationToken = default)
    {
        if ((await _storageProvider.OpenFolderPickerAsync(
                new FolderPickerOpenOptions
                {
                    AllowMultiple = false,
                })) is { Count: > 0} folders)
        {
            foreach (var folder in folders)
            {
                var localPathNullable = folder.TryGetLocalPath();
                _logger.LogDebug("Checking folder: {Path}", localPathNullable);
                if (localPathNullable is null)
                {
                    _logger.LogWarning("Folder {Path} is not accessible", folder.Path);
                    await _messageBoxProvider.ShowMessageBoxAsync(
                        new MessageBoxParams
                        {
                            Buttons = MessageBoxButtons.Ok,
                            Icon = MaterialIconKind.FolderRemoveOutline,
                            Message = "This folder is not accessible.",
                            Title = "Folder not accessible"
                        });
                    continue;
                }
                if (_mutableConfiguration.Current.LookupPaths.Contains(localPathNullable))
                {
                    _logger.LogWarning("Folder {Path} is already in the lookup paths", localPathNullable);
                    await _messageBoxProvider.ShowMessageBoxAsync(
                        new MessageBoxParams
                        {
                            Buttons = MessageBoxButtons.Ok,
                            Icon = MaterialIconKind.FolderRemoveOutline,
                            Message = "This folder is already in the lookup paths.",
                            Title = "Folder already in lookup paths"
                        });
                    continue;
                }
                _logger.LogDebug("Adding lookup path: {Path}", folder);
                await _mutableConfiguration.SetPropertyValueAsync(conf => conf.LookupPaths, _mutableConfiguration.Current.LookupPaths.Append(localPathNullable).ToArray(), cancellationToken);
            }
        }
    }

    [ReactiveCommand]
    private async Task OnNextFontSize(double obj)
    {
        _application.Resources[App.SystemFontSize] = obj;
        await _mutableConfiguration.SetPropertyValueAsync(conf => conf.FontSize, obj);
    }


    [ReactiveCommand]
    private async Task ClearWholeCache(CancellationToken cancellationToken = default)
    {
        var loggerConfiguration = LoggerConfiguration.Default;
        var cachePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppDomain.CurrentDomain.FriendlyName);
        if (await _messageBoxProvider.ShowValidatedInputAsync(
                StringsAndTexts.ApplicationSettingsViewModelAreYouSure,
                string.Format(
                    StringsAndTexts.ApplicationSettingsViewModelConfirmMessageBoxContent.Replace(
                        "\\n",
                        Environment.NewLine), cachePath,
                    StringsAndTexts.ApplicationSettingsViewModelConfirmDialogConfirmValue),
                inputToValidate =>
                {
                    ArgumentException.ThrowIfNullOrWhiteSpace(inputToValidate);
                    return string.Equals(
                        inputToValidate,
                        StringsAndTexts.ApplicationSettingsViewModelConfirmDialogConfirmValue, StringComparison.Ordinal)
                        ? null
                        : StringsAndTexts.ApplicationSettingsViewModelConfirmationError;
                }) is { IsConfirmed: false }) return;
        var stopWatch = Stopwatch.StartNew();
        foreach (var file in Directory.EnumerateFiles(cachePath, "*", SearchOption.AllDirectories))
            try
            {
                File.Delete(file);
                _logger.LogInformation("Deleted file: {File}", file);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error deleting file: {File}", file);
            }

        foreach (var directory in Directory.EnumerateDirectories(cachePath, "*", SearchOption.AllDirectories))
            try
            {
                if (directory == loggerConfiguration.LogFilePath) continue;
                Directory.Delete(directory, true);
                _logger.LogInformation("Deleted directory: {Directory}", directory);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error deleting directory: {Directory}", directory);
            }

        stopWatch.Stop();
        _logger.LogInformation("Cache cleared in {ElapsedTime} ms", stopWatch.Elapsed.Milliseconds);
    }

    [ReactiveCommand]
    private void DeleteOldLogFiles()
    {
        foreach (var logFile in LogFiles)
            try
            {
                if (!File.Exists(logFile)) continue;
                File.Delete(logFile);
                _logger.LogInformation("Deleted log file: {LogFile}", logFile);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to delete log file: {LogFile}", logFile);
            }

        LogFiles.Clear();
    }

    [ReactiveCommand]
    private Task<bool> OpenCacheFolder(CancellationToken token = default) => _launcher.LaunchDirectoryInfoAsync(
        new DirectoryInfo(
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                AppDomain.CurrentDomain.FriendlyName)));

    private async void OnNextTheme(ThemeVariant variant)
    {
        try
        {
            var themeVariant = variant switch
            {
                ThemeVariant.Dark => Avalonia.Styling.ThemeVariant.Dark,
                ThemeVariant.Light => Avalonia.Styling.ThemeVariant.Light,
                _ => Avalonia.Styling.ThemeVariant.Default
            };
            if (_application.ActualThemeVariant == themeVariant) return;
            _logger.LogDebug(
                "Changing Theme Variant from {OldThemeVariant} to {ThemeVariant}",
                _application.ActualThemeVariant.Key.ToString(), themeVariant.Key);
            _application.RequestedThemeVariant = themeVariant;
            await _mutableConfiguration.SetPropertyValueAsync(conf => conf.PreferredTheme, variant);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while changing Theme Variant from {OldThemeVariant} to {ThemeVariant}", _application.ActualThemeVariant.Key.ToString(), variant.ToString());
        }
    }

    private void OnNextDaysToDelete(int obj)
    {
        var logConfiguration = LoggerConfiguration.Default;
        LogFiles.Clear();
        foreach (var logFile in Directory.EnumerateFiles(
                     logConfiguration.LogFilePath, "*.log",
                     SearchOption.TopDirectoryOnly))
        {
            var fileName = Path.GetFileName(logFile).Replace(AppDomain.CurrentDomain.FriendlyName, string.Empty);
            if (fileName.Length < 8) continue;
            var extractedDate = fileName[..8];
            if (DateOnly.TryParseExact(extractedDate, "yyyyMMdd", out var dateTime) &&
                DateTime.Now.Subtract(dateTime.ToDateTime(TimeOnly.MinValue)) > TimeSpan.FromDays(obj))
                LogFiles.Add(logFile);
        }
    }

    private async Task OnNextLevel(LoggingLevelSwitchChangedEventArgs obj)
    {
        if (_levelSwitch.MinimumLevel == obj.NewLevel)
            return;
        _levelSwitch.MinimumLevel = obj.NewLevel;
        _logger.LogCritical("Log level changed from {OldLogLevel} to {NewLogLevel}", obj.OldLevel, obj.NewLevel);
    }
}