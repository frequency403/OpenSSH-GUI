using System.Collections.Specialized;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Reflection;
using Avalonia.Animation.Easings;
using Avalonia.Platform.Storage;
using JetBrains.Annotations;
using Material.Icons;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Interfaces.Hosts;
using OpenSSH_GUI.Core.Lib.Keys;
using OpenSSH_GUI.Core.MVVM;
using OpenSSH_GUI.Core.Resources.Wrapper;
using OpenSSH_GUI.Core.Services;
using OpenSSH_GUI.Dialogs.Enums;
using OpenSSH_GUI.Dialogs.Interfaces;
using OpenSSH_GUI.Dialogs.Models;
using OpenSSH_GUI.Resources;
using OpenSSH_GUI.Views;
using ReactiveUI;
using ReactiveUI.Avalonia;
using ReactiveUI.SourceGenerators;
using Renci.SshNet;
using SshNet.Keygen.Extensions;

namespace OpenSSH_GUI.ViewModels;

[UsedImplicitly]
public partial class MainWindowViewModel : ViewModelBase
{
    private static readonly string? ProjectUrl = Assembly.GetExecutingAssembly()
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .FirstOrDefault(a => a.Key == "ProjectUrl")?.Value;

    private readonly IDialogHost _dialogHost;
    private readonly ILauncher _launcher;
    private readonly ILogger<MainWindowViewModel> _logger;
    private readonly IMessageBoxProvider _messageBoxProvider;
    private readonly IServiceProvider _serviceProvider;

    [ObservableAsProperty(ReadOnly = true)]
    private string _itemsCountTooltip = string.Empty;

    [Reactive(SetModifier = AccessModifier.Private)]
    private string _version;

    [ObservableAsProperty(ReadOnly = true)]
    private string _windowTitle = string.Empty;

    [ObservableAsProperty(ReadOnly = true)]
    private bool _isProvidePasswordExecuting;

    public MainWindowViewModel(
        ILogger<MainWindowViewModel> logger,
        SshKeyManager sshKeyManager,
        ServerConnectionService serverConnectionService,
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        IMessageBoxProvider messageBoxProvider,
        ILauncher launcher,
        IDialogHost dialogHost)
    {
        SshKeyManager = sshKeyManager;
        ServerConnectionService = serverConnectionService;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _messageBoxProvider = messageBoxProvider;
        _launcher = launcher;
        _dialogHost = dialogHost;
        Version = configuration[Program.VersionEnvVar] ?? "VERSION ERROR";

        _windowTitleHelper = this.WhenAnyValue(vm => vm.Version)
            .Select(v => string.Join(" v", Program.AppName, v))
            .ToProperty(this, vm => vm.WindowTitle)
            .DisposeWith(Disposables);

        var sshKeysCountChanged = Observable
            .FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                h => ((INotifyCollectionChanged)SshKeyManager.SshKeys).CollectionChanged += h,
                h => ((INotifyCollectionChanged)SshKeyManager.SshKeys).CollectionChanged -= h)
            .Select(_ => SshKeyManager.SshKeys.Count)
            .StartWith(SshKeyManager.SshKeys.Count)
            .ObserveOn(AvaloniaScheduler.Instance);

        _itemsCountTooltipHelper = sshKeysCountChanged
            .Select(count => string.Format(StringsAndTexts.MainWindowFoundKeyPairsCountLabel, count))
            .ToProperty(this, vm => vm.ItemsCountTooltip)
            .DisposeWith(Disposables);
        _isProvidePasswordExecutingHelper = ProvidePasswordCommand.IsExecuting
            .ToProperty(this, vm => vm.IsProvidePasswordExecuting)
            .DisposeWith(Disposables);
    }

    public ServerConnectionService ServerConnectionService { get; }
    public SshKeyManager SshKeyManager { get; }

    [ReactiveCommand]
    private Task OpenApplicationSettingsWindowAsync(CancellationToken cancellationToken = default) =>
        OpenWindow<ApplicationSettingsWindow, ApplicationSettingsViewModel>(cancellationToken);

    [ReactiveCommand]
    private Task OpenFileInfoWindowAsync(SshKeyFileSource source, CancellationToken cancellationToken = default) =>
        OpenWindow<FileInfoWindow, FileInfoWindowViewModel, SshKeyFileSource>(
            source,
            cancellationToken);

    [ReactiveCommand]
    private Task OpenCreateKeyWindowAsync(CancellationToken cancellationToken = default) => OpenWindow<AddKeyWindow, AddKeyWindowViewModel>(cancellationToken);

    [ReactiveCommand]
    private Task OpenEditAuthorizedKeysWindowAsync(CancellationToken cancellationToken = default) =>
        OpenWindow<EditAuthorizedKeysWindow, EditAuthorizedKeysViewModel>(cancellationToken);

    [ReactiveCommand]
    private Task OpenEditKnownHostsWindowAsync(CancellationToken cancellationToken = default) => OpenWindow<EditKnownHostsWindow, EditKnownHostsWindowViewModel>(cancellationToken);

    [ReactiveCommand]
    private Task OpenConnectToServerWindowAsync(CancellationToken cancellationToken = default) => OpenWindow<ConnectToServerWindow, ConnectToServerViewModel>(cancellationToken);

    [ReactiveCommand]
    private void ResetKey(SshKeyFile keyFile)
    {
        try
        {
            keyFile.Reset();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unhandled error during key reset");
        }
    }

    [ReactiveCommand]
    private async Task ReloadKeysAsync(CancellationToken cancellationToken = default)
    {
        switch (await SshKeyManager.RerunSearchAsync(cancellationToken))
        {
            case { IsSuccess: false } x:
                await _messageBoxProvider.ShowMessageBoxAsync(StringsAndTexts.Error, x.Exception.Message);
                break;
            default:
                _logger.LogInformation("Keys reloaded");
                break;
        }
    }

    [ReactiveCommand]
    private Task ShowPrivateKeyExportWindow(SshKeyFile key, CancellationToken token = default)
    {
        PrivateKeyFile? keyFile = key;
        var content = string.Empty;
        switch (key)
        {
            case null or { NeedsPassword: true, Password.IsValid: false }:
                _logger.LogError("Keyfile is null");
                return Task.CompletedTask;
            case { NeedsPassword: false, Password.IsValid: true }:
            {
                if (keyFile is not null)
                    content = keyFile.ToOpenSshFormat(key.Password.GetPasswordString());
                break;
            }
            default:
            {
                if (keyFile is not null)
                    content = keyFile.ToOpenSshFormat();
                break;
            }
        }

        return ShowExportWindow(key, content, null, token);
    }

    [ReactiveCommand]
    private Task ShowPublicKeyExportWindow(SshKeyFile key, CancellationToken token = default)
    {
        PrivateKeyFile? keyFile = key;
        return keyFile != null
            ? ShowExportWindow(key, keyFile.ToOpenSshFormat(), null, token)
            : _messageBoxProvider.ShowMessageBoxAsync(
                StringsAndTexts.Error,
                StringsAndTexts.MainWindowViewModelExportKeyErrorMessage);
    }

    private async Task ShowExportWindow(SshKeyFile key, string content, string? windowTitle = null,
        CancellationToken token = default)
    {
        windowTitle ??= string.Format(
            StringsAndTexts.MainWindowViewModelDynamicExportWindowTitle,
            key.KeyType, key.FileName);
        if (string.IsNullOrWhiteSpace(content))
        {
            await _messageBoxProvider.ShowMessageBoxAsync(
                StringsAndTexts.Error,
                StringsAndTexts.MainWindowViewModelExportKeyErrorMessage);
            return;
        }

        var view = await _serviceProvider
            .ResolveViewAsync<ExportWindow, ExportWindowViewModel, (string WindowTitle, string Export)>(
                new ValueTuple<string, string>
                {
                    Item1 = windowTitle,
                    Item2 = content
                }, token: token);
        await _dialogHost.ShowDialog(view);
    }

    [ReactiveCommand]
    private async Task ShowNotImplementedMessageBoxAsync(CancellationToken cancellationToken = default)
    {
        await _messageBoxProvider.ShowMessageBoxAsync(
            StringsAndTexts.NotImplementedBoxTitle,
            StringsAndTexts.NotImplementedBoxText, MessageBoxButtons.Ok, MaterialIconKind.InformationOutline);
    }

    [ReactiveCommand]
    private async Task OpenBrowserAsync(int commandTypeParameter, CancellationToken cancellationToken = default)
    {
        if (ProjectUrl is null)
            return;
        var uriBuilder = new UriBuilder(ProjectUrl);
        switch (commandTypeParameter)
        {
            case 1:
                uriBuilder.Path += "/issues";
                break;
            case 2:
                uriBuilder.Query = "tab=readme-ov-file";
                uriBuilder.Fragment = "authors";
                break;
        }

        await _launcher.LaunchUriAsync(uriBuilder.Uri);
    }

    [ReactiveCommand]
    private async Task DisconnectFromServerAsync(CancellationToken cancellationToken)
    {
        var messageBoxText = StringsAndTexts.MainWindowDisconnectBoxTextSuccess;
        var messageBoxIcon = MaterialIconKind.InformationOutline;
        if (ServerConnectionService.IsConnected)
        {
            try
            {
                await ServerConnectionService.CloseConnection(true, cancellationToken);
            }
            catch (Exception exception)
            {
                messageBoxText = exception.Message;
                messageBoxIcon = MaterialIconKind.ErrorOutline;
            }
        }
        else
        {
            messageBoxText = StringsAndTexts.MainWindowDisconnectBoxTextNone;
            messageBoxIcon = MaterialIconKind.ErrorOutline;
        }

        await _messageBoxProvider.ShowMessageBoxAsync(
            StringsAndTexts.MainWindowDisconnectBoxTitle, messageBoxText,
            MessageBoxButtons.Ok, messageBoxIcon);
    }


    [ReactiveCommand]
    private async Task DeleteKeyAsync(SshKeyFile sshKeyFile, CancellationToken cancellationToken = default)
    {
        if (await _messageBoxProvider.ShowMessageBoxAsync(
                string.Format(StringsAndTexts.MainWindowViewModelDeleteKeyTitleText, sshKeyFile.FileName),
                StringsAndTexts.MainWindowViewModelDeleteKeyQuestionTextPair, MessageBoxButtons.YesNo,
                MaterialIconKind.QuestionBoxOutline) is MessageBoxResult.Yes)
            if (await SshKeyManager.TryDeleteKeyAsync(sshKeyFile, cancellationToken) is
                { IsSuccess: false, Exception: { } error })
                await _messageBoxProvider.ShowMessageBoxAsync(
                    string.Format(StringsAndTexts.MainWindowViewModelDeleteKeyTitleText, sshKeyFile.FileName)
                    , error.Message);
    }

    [ReactiveCommand]
    private async Task ProvidePasswordAsync(SshKeyFile key, CancellationToken cancellationToken = default)
    {
        if (!await _messageBoxProvider.ShowRetryMessageBoxAsync(
                async () =>
                {
                    using var secureInputResult = await _messageBoxProvider.ShowSecureInputAsync(
                        new SecureInputParams
                        {
                            Title = StringsAndTexts.MainWindowViewModelProvidePasswordPromptHeading,
                            Prompt = string.Join(
                                Environment.NewLine,
                                StringsAndTexts.MainWindowViewModelProvidePasswordPromptBodyHeading,
                                Path.GetFileName(key.AbsoluteFilePath))
                        });
                    bool? operationResult = secureInputResult switch
                    {
                        null => null,
                        { Value.Length: <= 0 } => true,
                        { Value.Length: > 0 } => key.SetPassword(secureInputResult.Value.Span)
                    };
                    if (operationResult is false)
                        key.Reset();
                    return operationResult;
                }, StringsAndTexts.MainWindowViewModelProvidePasswordErrorHeading,
                StringsAndTexts.MainWindowViewModelProvidePasswordErrorContent,
                retries: 3, showTryCountInTitle: true, icon: MaterialIconKind.WarningOutline))
            await _messageBoxProvider.ShowErrorMessageBoxAsync(
                customMessage: string.Join(
                    " ", "Key", key.FileName,
                    "could not be opened correctly"));
    }

    private async Task OpenWindow<TWindow, TViewModel, TInitializer>(TInitializer param,
        CancellationToken token = default)
        where TWindow : WindowBase<TViewModel, TInitializer>
        where TViewModel : ViewModelBase<TInitializer>
    {
        await _dialogHost.ShowDialog(
            await _serviceProvider.ResolveViewAsync<TWindow, TViewModel, TInitializer>(param, token: token)
        );
    }

    private async Task OpenWindow<TWindow, TViewModel>(CancellationToken token = default)
        where TWindow : WindowBase<TViewModel>
        where TViewModel : ViewModelBase
    {
        await _dialogHost.ShowDialog(
            await _serviceProvider.ResolveViewAsync<TWindow, TViewModel>(token: token));
    }
}