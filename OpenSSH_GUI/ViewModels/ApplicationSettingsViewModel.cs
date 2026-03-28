using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.MVVM;
using OpenSSH_GUI.Dialogs.Interfaces;
using OpenSSH_GUI.Resources;
using ReactiveUI;
using ReactiveUI.Avalonia;
using ReactiveUI.SourceGenerators;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace OpenSSH_GUI.ViewModels;

[UsedImplicitly]
public partial class ApplicationSettingsViewModel : ViewModelBase<ApplicationSettingsViewModel>
{
    private readonly ILogger<ApplicationSettingsViewModel> _logger;
    private readonly IMessageBoxProvider _messageBoxProvider;
    private readonly LoggingLevelSwitch _levelSwitch;
    public static LogEventLevel[] AvailableLogLevels { get; }= Enum.GetValues<LogEventLevel>();
    public static int[] DaysToDelete { get; } = Enumerable.Range(1, 4).Select(i => i * 7).ToArray();
    
    [Reactive]
    private LogEventLevel _currentLogLevel;
    
    [Reactive]
    private int _daysToDeleteSelected;
    
    public ObservableCollection<string> LogFiles { get; } = [];
    
    [Reactive] 
    private bool _canDeleteOldLogFiles;
    
    public ApplicationSettingsViewModel(ILogger<ApplicationSettingsViewModel> logger, IMessageBoxProvider messageBoxProvider, LoggingLevelSwitch levelSwitch)
    {
        _logger = logger;
        _messageBoxProvider = messageBoxProvider;
        _levelSwitch = levelSwitch;
        CurrentLogLevel = levelSwitch.MinimumLevel;
        this.WhenAnyValue(model => model.CurrentLogLevel)
            .ObserveOn(AvaloniaScheduler.Instance)
            .DistinctUntilChanged()
            .Subscribe(OnNext).DisposeWith(Disposables);
        
        this.WhenAnyValue(model => model.DaysToDeleteSelected)
            .Subscribe(OnNext).DisposeWith(Disposables);

        this.WhenAnyValue(vm => vm.LogFiles.Count)
            .DistinctUntilChanged()
            .Subscribe(count =>
            {
                CanDeleteOldLogFiles = count > 0;
            })
            .DisposeWith(Disposables);
        
        DaysToDeleteSelected = DaysToDelete[0];
    }
    

    [ReactiveCommand]
    private async Task ClearWholeCache(CancellationToken cancellationToken = default)
    {
        var loggerConfiguration = Core.Configuration.LoggerConfiguration.Default;
        var cachePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppDomain.CurrentDomain.FriendlyName);
        if ((await _messageBoxProvider.ShowValidatedInputAsync(StringsAndTexts.ApplicationSettingsViewModelAreYouSure,
                string.Format(StringsAndTexts.ApplicationSettingsViewModelConfirmMessageBoxContent.Replace("\\n", Environment.NewLine), cachePath, StringsAndTexts.ApplicationSettingsViewModelConfirmDialogConfirmValue),
                inputToValidate =>
                {
                    ArgumentException.ThrowIfNullOrWhiteSpace(inputToValidate);
                    return string.Equals(inputToValidate, StringsAndTexts.ApplicationSettingsViewModelConfirmDialogConfirmValue, StringComparison.Ordinal)
                        ? null
                        : StringsAndTexts.ApplicationSettingsViewModelConfirmationError;
                })) is { IsConfirmed: false }) return;
        var stopWatch = Stopwatch.StartNew();
        foreach (var file in Directory.EnumerateFiles(cachePath, "*", SearchOption.AllDirectories))
        {
            try
            {
                File.Delete(file);
                _logger.LogInformation("Deleted file: {File}", file);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error deleting file: {File}", file);
            }
        }
        foreach (var directory in Directory.EnumerateDirectories(cachePath, "*", SearchOption.AllDirectories))
        {
            try
            {
                if(directory == loggerConfiguration.LogFilePath) continue;
                Directory.Delete(directory, true);
                _logger.LogInformation("Deleted directory: {Directory}", directory);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error deleting directory: {Directory}", directory);
            }
        }
        stopWatch.Stop();
        _logger.LogInformation("Cache cleared in {ElapsedTime} ms", stopWatch.Elapsed.Milliseconds);
    }
    
    [ReactiveCommand]
    private void DeleteOldLogFiles()
    {
        foreach (var logFile in LogFiles)
        {
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
        }
        LogFiles.Clear();
    }

    private void OnNext(int obj)
    {
        _logger.LogDebug("Days to delete selected: {Days}", obj);
        var logConfiguration = Core.Configuration.LoggerConfiguration.Default;
        LogFiles.Clear();
        foreach (var logFile in Directory.EnumerateFiles(logConfiguration.LogFilePath, "*.log", SearchOption.TopDirectoryOnly))
        {
            var extractedDate = Path.GetFileName(logFile).Replace(AppDomain.CurrentDomain.FriendlyName, string.Empty)[..8];
            if(DateOnly.TryParseExact(extractedDate, "yyyyMMdd", out var dateTime) && DateTime.Now.Subtract(dateTime.ToDateTime(TimeOnly.MinValue)) > TimeSpan.FromDays(obj))
                LogFiles.Add(logFile);
        }
    }

    private void OnNext(LogEventLevel obj)
    {
        _levelSwitch.MinimumLevel = obj;
        _logger.LogCritical("Log level changed to {LogLevel}", obj);
    }
}