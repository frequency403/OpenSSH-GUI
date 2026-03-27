using System.Reactive;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Reflection;
using System.Text.Encodings.Web;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using DryIoc;
using JetBrains.Annotations;
using Material.Icons;
using Material.Icons.Avalonia;
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
using ReactiveUI.SourceGenerators;
using Renci.SshNet;
using SshNet.Keygen.Extensions;

namespace OpenSSH_GUI.ViewModels;

[UsedImplicitly]
public partial class MainWindowViewModel : ViewModelBase<MainWindowViewModel>
{
    private static readonly string? ProjectUrl = Assembly.GetExecutingAssembly()
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .FirstOrDefault(a => a.Key == "ProjectUrl")?.Value;

    private readonly IDialogHost _dialogHost;
    private readonly IMessageBoxProvider _messageBoxProvider;
    private readonly ILauncher _launcher;
    private readonly IResolver _serviceProvider;

    public MainWindowViewModel(
        ILogger<MainWindowViewModel> logger,
        SshKeyManager sshKeyManager,
        ServerConnectionService serverConnectionService,
        IResolver serviceProvider,
        IConfiguration configuration,
        IMessageBoxProvider messageBoxProvider,
        ILauncher launcher,
        IDialogHost dialogHost) : base(logger)
    {
        SshKeyManager = sshKeyManager;
        ServerConnectionService = serverConnectionService;
        _serviceProvider = serviceProvider;
        _messageBoxProvider = messageBoxProvider;
        _launcher = launcher;
        _dialogHost = dialogHost;
        Version = configuration[Program.VersionEnvVar] ?? "VERSION ERROR";

        _itemsCountIconHelper = this.WhenAnyValue(vm => vm.SshKeyManager.SshKeys.Count)
            .Select(GetMaterialNumericIcon)
            .ToProperty(this, vm => vm.ItemsCountIcon).DisposeWith(Disposables);

        _windowTitleHelper = this.WhenAnyValue(vm => vm.Version)
            .Select(ver => string.Join("-", Program.AppName, ver))
            .ToProperty(this, vm => vm.WindowTitle).DisposeWith(Disposables);

        _keyTypeSortDirectionIconHelper =
            this.WhenAnyValue(vm => vm.KeyTypeSort)
                .Select(EvaluateSortIconKind)
                .ToProperty(this, vm => vm.KeyTypeSortDirectionIcon)
                .DisposeWith(Disposables);

        _commentSortDirectionIconHelper = this.WhenAnyValue(vm => vm.CommentSort)
            .Select(EvaluateSortIconKind)
            .ToProperty(this, vm => vm.CommentSortDirectionIcon).DisposeWith(Disposables);

        _fingerPrintSortDirectionIconHelper = this.WhenAnyValue(vm => vm.FingerPrintSort)
            .Select(EvaluateSortIconKind)
            .ToProperty(this, vm => vm.FingerPrintSortDirectionIcon).DisposeWith(Disposables);

        this.WhenAnyValue(vm => vm.KeyTypeSort)
            .Subscribe(sort => SshKeyManager.ChangeOrder(sort switch
            {
                null => key => key.OrderBy(e => e.FileName),
                true => key => key.OrderBy(e => e.KeyType),
                false => key => key.OrderByDescending(e => e.KeyType)
            })).DisposeWith(Disposables);
        
        this.WhenAnyValue(vm => vm.CommentSort)
            .Subscribe(sort => SshKeyManager.ChangeOrder(sort switch
            {
                null => key => key.OrderBy(e => e.FileName),
                true => key => key.OrderBy(e => e.Comment),
                false => key => key.OrderByDescending(e => e.Comment)
            })).DisposeWith(Disposables);
        
        this.WhenAnyValue(vm => vm.FingerPrintSort)
            .Subscribe(sort => SshKeyManager.ChangeOrder(sort switch
            {
                null => key => key.OrderBy(e => e.FileName),
                true => key => key.OrderBy(e => e.Fingerprint),
                false => key => key.OrderByDescending(e => e.Fingerprint)
            })).DisposeWith(Disposables);
    }

    public ServerConnectionService ServerConnectionService { get; }
    public SshKeyManager SshKeyManager { get; }

    [Reactive] private string _version;
    [Reactive] private bool? _keyTypeSort;
    [Reactive] private bool? _commentSort;
    [Reactive] private bool? _fingerPrintSort;

    [ObservableAsProperty] private MaterialIcon _itemsCountIcon = new() { Kind = MaterialIconKind.Infinity };
    [ObservableAsProperty] private string _windowTitle = string.Empty;
    [ObservableAsProperty] private MaterialIconKind _keyTypeSortDirectionIcon = MaterialIconKind.CircleOutline;
    [ObservableAsProperty] private MaterialIconKind _commentSortDirectionIcon = MaterialIconKind.CircleOutline;
    [ObservableAsProperty] private MaterialIconKind _fingerPrintSortDirectionIcon = MaterialIconKind.CircleOutline;

    [ReactiveCommand]
    private Task OpenApplicationSettingsWindowAsync(CancellationToken cancellationToken = default) =>
        OpenWindow<ApplicationSettingsWindow, ApplicationSettingsViewModel>(cancellationToken);

    [ReactiveCommand]
    private Task OpenFileInfoWindowAsync(string path, CancellationToken cancellationToken = default) =>
        OpenWindow<FileInfoWindow, FileInfoWindowViewModel, string, FileInfoViewModelInitializer>(path,
            cancellationToken);

    [ReactiveCommand]
    private Task OpenCreateKeyWindowAsync(CancellationToken cancellationToken = default) =>
        OpenWindow<AddKeyWindow, AddKeyWindowViewModel>(cancellationToken);

    [ReactiveCommand]
    private Task OpenEditAuthorizedKeysWindowAsync(CancellationToken cancellationToken = default) =>
        OpenWindow<EditAuthorizedKeysWindow, EditAuthorizedKeysViewModel>(cancellationToken);

    [ReactiveCommand]
    private Task OpenEditKnownHostsWindowAsync(CancellationToken cancellationToken = default) =>
        OpenWindow<EditKnownHostsWindow, EditKnownHostsWindowViewModel>(cancellationToken);

    [ReactiveCommand]
    private Task OpenConnectToServerWindowAsync(CancellationToken cancellationToken = default) =>
        OpenWindow<ConnectToServerWindow, ConnectToServerViewModel>(cancellationToken);

    [ReactiveCommand]
    private void ResetKey(SshKeyFile keyFile)
    {
        try
        {
            keyFile.Reset();
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Unhandled error during key reset");
        }
    }

    [ReactiveCommand]
    private Task ReloadKeysAsync(CancellationToken cancellationToken = default) => SshKeyManager.RerunSearchAsync();

    [ReactiveCommand]
    private async Task ChangeFilenameAsync(SshKeyFile key, CancellationToken token = default)
    {
        var validatedInputResult = await _messageBoxProvider.ShowValidatedInputAsync(new ValidatedInputParams
        {
            Buttons = MessageBoxButtons.OkCancel,
            Icon = MaterialIconKind.FileEditOutline,
            InitialValue = key.FileName ?? string.Empty,
            Message = "ChangeMe",
            Prompt = "EnterNewFilename",
            Watermark = "Enter new filename",
            Validator = argument =>
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(argument);
                return SshKeyManager.SshKeys.Any(k => k.FileName == argument) ? "Filename already exists" : null;
            }
        });
        if (validatedInputResult is { IsConfirmed: true, Value: { Length: > 0 } filename })
            await SshKeyManager.RenameKeyAsync(key, filename, token);
    }

    [ReactiveCommand]
    private Task ShowPrivateKeyExportWindow(SshKeyFile key, CancellationToken token = default)
    {
        PrivateKeyFile? keyFile = key;
        var content = string.Empty;
        switch (key)
        {
            case null or { NeedsPassword: true, Password.IsValid: false }:
                Logger.LogError("Keyfile is null");
                return Task.CompletedTask;
            case { NeedsPassword: false, Password.IsValid: true } passwordProtectedKeyFile:
            {
                keyFile = passwordProtectedKeyFile;
                if (keyFile is not null)
                    content = keyFile.ToOpenSshFormat(passwordProtectedKeyFile.Password.GetPasswordString());
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
        var content = keyFile is not null ? keyFile.ToOpenSshPublicFormat() : string.Empty;
        return ShowExportWindow(key, content, null, token);
    }

    [ReactiveCommand]
    private async Task ShowExportWindow(SshKeyFile key, string content, string? windowTitle = null,
        CancellationToken token = default)
    {
        windowTitle ??= string.Format(StringsAndTexts.MainWindowViewModelDynamicExportWindowTitle,
            key.HashAlgorithmName, key.FileName);
        if (string.IsNullOrWhiteSpace(content))
        {
            await _messageBoxProvider.ShowMessageBoxAsync(StringsAndTexts.Error,
                StringsAndTexts.MainWindowViewModelExportKeyErrorMessage, MessageBoxButtons.Ok, MessageBoxIcon.Error);
            return;
        }

        var view = await _serviceProvider
            .ResolveViewAsync<ExportWindow, ExportWindowViewModel, ExportWindowViewModelInitializerParameters>(
                new ExportWindowViewModelInitializerParameters
                {
                    Export = content,
                    WindowTitle = windowTitle
                }, token: token);
        await _dialogHost.ShowDialog(view);
    }

    [ReactiveCommand]
    private async Task ShowNotImplementedMessageBoxAsync(CancellationToken cancellationToken = default)
    {
        await _messageBoxProvider.ShowMessageBoxAsync(StringsAndTexts.NotImplementedBoxTitle,
            StringsAndTexts.NotImplementedBoxText, MessageBoxButtons.Ok, MessageBoxIcon.Information);
    }

    [ReactiveCommand]
    private async Task OpenBrowserAsync(int commandTypeParameter, CancellationToken cancellationToken = default)
    {
        if (commandTypeParameter switch
            {
                1 => string.Join("/", ProjectUrl, "issues"),
                2 => string.Join("#", ProjectUrl, "authors"),
                _ => ProjectUrl
            } is { Length: > 0 } url)
            await _launcher.LaunchUriAsync(new Uri(HtmlEncoder.Default.Encode(url)));
    }

    [ReactiveCommand]
    private async Task DisconnectFromServerAsync(CancellationToken cancellationToken)
    {
        var messageBoxText = StringsAndTexts.MainWindowDisconnectBoxTextSuccess;
        var messageBoxIcon = MessageBoxIcon.Information;
        if (ServerConnectionService.IsConnected)
        {
            try
            {
                await ServerConnectionService.CloseConnection(true, cancellationToken);
            }
            catch (Exception exception)
            {
                messageBoxText = exception.Message;
                messageBoxIcon = MessageBoxIcon.Error;
            }
        }
        else
        {
            messageBoxText = StringsAndTexts.MainWindowDisconnectBoxTextNone;
            messageBoxIcon = MessageBoxIcon.Error;
        }

        await _messageBoxProvider.ShowMessageBoxAsync(StringsAndTexts.MainWindowDisconnectBoxTitle, messageBoxText,
            MessageBoxButtons.Ok, messageBoxIcon);
    }


    [ReactiveCommand]
    private async Task DeleteKeyAsync(SshKeyFile sshKeyFile, CancellationToken cancellationToken = default)
    {
        if (await _messageBoxProvider.ShowMessageBoxAsync(
                string.Format(StringsAndTexts.MainWindowViewModelDeleteKeyTitleText, sshKeyFile.FileName),
                StringsAndTexts.MainWindowViewModelDeleteKeyQuestionTextPair, MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) is MessageBoxResult.Yes)
            if ((await SshKeyManager.TryDeleteKeyAsync(sshKeyFile, cancellationToken)) is
                { success: false, exception: { } error })
                await _messageBoxProvider.ShowMessageBoxAsync(
                    string.Format(StringsAndTexts.MainWindowViewModelDeleteKeyTitleText, sshKeyFile.FileName)
                    , error.Message, MessageBoxButtons.Ok, MessageBoxIcon.Error);
    }

    [ReactiveCommand]
    private async Task ProvidePasswordAsync(SshKeyFile key, CancellationToken cancellationToken = default)
    {
        var trys = 0;

        while (key.NeedsPassword && trys < 3)
        {
            using var secureInputResult = await _messageBoxProvider.ShowSecureInputAsync(
                StringsAndTexts.MainWindowViewModelProvidePasswordPromptHeading,
                string.Format(StringsAndTexts.MainWindowViewModelProvidePasswordPromptBodyHeading,
                    Path.GetFileName(key.AbsoluteFilePath)));
            if (secureInputResult != null && key.SetPassword(secureInputResult.Value.Span))
                return;


            if (await _messageBoxProvider.ShowMessageBoxAsync(
                    StringsAndTexts.MainWindowViewModelProvidePasswordErrorHeading, string.Format(
                        StringsAndTexts.MainWindowViewModelProvidePasswordErrorContent,
                        trys + 1, 3), MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning) is MessageBoxResult
                    .Cancel)
                break;
            trys++;
        }
    }

    private async Task OpenWindow<TWindow, TViewModel, TParam, TInitializer>(TParam param,
        CancellationToken token = default)
        where TWindow : WindowBase<TViewModel, TInitializer>
        where TViewModel : ViewModelBase<TViewModel, TInitializer>
        where TInitializer : class, IInitializerParameters<TViewModel>

    {
        var initializer = Activator.CreateInstance<TInitializer>();
        foreach (var property in initializer.GetType().GetProperties()
                     .Where(pi => pi.PropertyType == typeof(TParam) && pi.CanWrite))
            property.SetValue(initializer, param);

        await _dialogHost.ShowDialog<TWindow, TViewModel>(
            await _serviceProvider.ResolveViewAsync<TWindow, TViewModel, TInitializer>(initializer, token: token)
        );
    }

    private async Task OpenWindow<TWindow, TViewModel>(CancellationToken token = default)
        where TWindow : WindowBase<TViewModel>
        where TViewModel : ViewModelBase<TViewModel>
    {
        await _dialogHost.ShowDialog<TWindow, TViewModel>(
            await _serviceProvider.ResolveViewAsync<TWindow, TViewModel>(token: token));
    }

    private static MaterialIcon GetMaterialNumericIcon(int count) => new()
    {
        Kind = count switch
        {
            0 => MaterialIconKind.NumericZero,
            1 => MaterialIconKind.NumericOne,
            2 => MaterialIconKind.NumericTwo,
            3 => MaterialIconKind.NumericThree,
            4 => MaterialIconKind.NumericFour,
            5 => MaterialIconKind.NumericFive,
            6 => MaterialIconKind.NumericSix,
            7 => MaterialIconKind.NumericSeven,
            8 => MaterialIconKind.NumericEight,
            9 => MaterialIconKind.NumericNine,
            10 => MaterialIconKind.Numeric10,
            _ => MaterialIconKind.Infinity
        },
        Width = 20,
        Height = 20
    };

    private static MaterialIconKind EvaluateSortIconKind(bool? value) =>
        value switch
        {
            null => MaterialIconKind.CircleOutline,
            true => MaterialIconKind.ChevronDownCircleOutline,
            false => MaterialIconKind.ChevronUpCircleOutline
        };
    
}