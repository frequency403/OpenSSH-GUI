using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using Avalonia;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Configuration;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.MVVM;
using OpenSSH_GUI.Dialogs.Interfaces;
using OpenSSH_GUI.Resources;
using ReactiveUI;
using ReactiveUI.Avalonia;
using ReactiveUI.SourceGenerators;
using Serilog.Core;
using Serilog.Events;

namespace OpenSSH_GUI.ViewModels;

[UsedImplicitly]
public partial class ApplicationSettingsViewModel : ViewModelBase<ApplicationSettingsViewModel>
{
    private readonly Application _application;
    private readonly LoggingLevelSwitch _levelSwitch;
    private readonly ILogger<ApplicationSettingsViewModel> _logger;
    private readonly IMessageBoxProvider _messageBoxProvider;

    [Reactive] private bool _canDeleteOldLogFiles;

    [Reactive] private LogEventLevel _currentLogLevel;

    [Reactive] private ThemeVariant _currentThemeVariant;

    [Reactive] private int _daysToDeleteSelected;
    
    [Reactive] private double fontSize = 12;

    [ObservableAsProperty] private string _cleanupFilesButtonText = string.Empty;

    public ApplicationSettingsViewModel(ILogger<ApplicationSettingsViewModel> logger,
        IMessageBoxProvider messageBoxProvider,
        Application application,
        LoggingLevelSwitch levelSwitch)
    {
        _logger = logger;
        _messageBoxProvider = messageBoxProvider;
        _levelSwitch = levelSwitch;
        _currentLogLevel = levelSwitch.MinimumLevel;
        _application = application;
        _daysToDeleteSelected = DaysToDelete[0];

        if (Enum.TryParse<ThemeVariant>(application.ActualThemeVariant.Key.ToString(), true, out var themeVariant))
            _currentThemeVariant = themeVariant;

        Observable
            .FromEventPattern<LoggingLevelSwitchChangedEventArgs>(
                handler => levelSwitch.MinimumLevelChanged += handler,
                handler => levelSwitch.MinimumLevelChanged -= handler
            )
            .Select(pattern => pattern.EventArgs)
            .Subscribe(OnNextLevel)
            .DisposeWith(Disposables);

        this.WhenAnyValue(model => model.CurrentLogLevel)
            .ObserveOn(AvaloniaScheduler.Instance)
            .DistinctUntilChanged()
            .Subscribe(level => OnNextLevel(new LoggingLevelSwitchChangedEventArgs(_levelSwitch.MinimumLevel, level)))
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

        _cleanupFilesButtonTextHelper = this.WhenAnyValue(vm => vm.LogFiles.Count)
            .ObserveOn(AvaloniaScheduler.Instance)
            .Select(count => string.Format(StringsAndTexts.ApplicationSettingsCleanupFiles, count))
            .ToProperty(this, vm => vm.CleanupFilesButtonText)
            .DisposeWith(Disposables);

        this.WhenAnyValue(vm => vm.CurrentThemeVariant)
            .ObserveOn(AvaloniaScheduler.Instance)
            .DistinctUntilChanged()
            .Subscribe(OnNextTheme)
            .DisposeWith(Disposables);
        
        this.WhenAnyValue(vm => vm.FontSize)
            .ObserveOn(AvaloniaScheduler.Instance)
            .DistinctUntilChanged()
            .Subscribe(OnNextFontSize)
            .DisposeWith(Disposables);
    }

    [ReactiveCommand]
    private void OnNextFontSize(double obj)
    {
        _application.Resources[App.SystemFontSize] = obj;
    }

    public static LogEventLevel[] AvailableLogLevels { get; } = Enum.GetValues<LogEventLevel>();
    public static ThemeVariant[] ThemeVariants { get; } = Enum.GetValues<ThemeVariant>();
    public static int[] DaysToDelete { get; } = Enumerable.Range(1, 4).Select(i => i * 7).ToArray();

    public ObservableCollection<string> LogFiles { get; } = [];


    [ReactiveCommand]
    private async Task ClearWholeCache(CancellationToken cancellationToken = default)
    {
        var loggerConfiguration = LoggerConfiguration.Default;
        var cachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppDomain.CurrentDomain.FriendlyName);
        if (await _messageBoxProvider.ShowValidatedInputAsync(StringsAndTexts.ApplicationSettingsViewModelAreYouSure,
                string.Format(
                    StringsAndTexts.ApplicationSettingsViewModelConfirmMessageBoxContent.Replace("\\n",
                        Environment.NewLine), cachePath,
                    StringsAndTexts.ApplicationSettingsViewModelConfirmDialogConfirmValue),
                inputToValidate =>
                {
                    ArgumentException.ThrowIfNullOrWhiteSpace(inputToValidate);
                    return string.Equals(inputToValidate,
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

    private void OnNextTheme(ThemeVariant variant)
    {
        var themeVariant = variant switch
        {
            ThemeVariant.Dark => Avalonia.Styling.ThemeVariant.Dark,
            ThemeVariant.Light => Avalonia.Styling.ThemeVariant.Light,
            _ => Avalonia.Styling.ThemeVariant.Default
        };
        if (_application.ActualThemeVariant == themeVariant) return;
        _logger.LogDebug("Changing Theme Variant from {OldThemeVariant} to {ThemeVariant}",
            _application.ActualThemeVariant.Key.ToString(), themeVariant.Key);
        _application.RequestedThemeVariant = themeVariant;
    }

    private void OnNextDaysToDelete(int obj)
    {
        _logger.LogDebug("Days to delete selected: {Days}", obj);
        var logConfiguration = LoggerConfiguration.Default;
        LogFiles.Clear();
        foreach (var logFile in Directory.EnumerateFiles(logConfiguration.LogFilePath, "*.log",
                     SearchOption.TopDirectoryOnly))
        {
            var extractedDate =
                Path.GetFileName(logFile).Replace(AppDomain.CurrentDomain.FriendlyName, string.Empty)[..8];
            if (DateOnly.TryParseExact(extractedDate, "yyyyMMdd", out var dateTime) &&
                DateTime.Now.Subtract(dateTime.ToDateTime(TimeOnly.MinValue)) > TimeSpan.FromDays(obj))
                LogFiles.Add(logFile);
        }
    }

    private void OnNextLevel(LoggingLevelSwitchChangedEventArgs obj)
    {
        if (_levelSwitch.MinimumLevel == obj.NewLevel)
            return;
        _levelSwitch.MinimumLevel = obj.NewLevel;
        _logger.LogCritical("Log level changed from {OldLogLevel} to {NewLogLevel}", obj.OldLevel, obj.NewLevel);
    }
}