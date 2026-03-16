using System.Diagnostics;
using System.Reactive;
using System.Reflection;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Material.Icons;
using Material.Icons.Avalonia;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Interfaces.Hosts;
using OpenSSH_GUI.Core.Interfaces.Services;
using OpenSSH_GUI.Core.Lib.Keys;
using OpenSSH_GUI.Core.MVVM;
using OpenSSH_GUI.Core.Resources.Wrapper;
using OpenSSH_GUI.Core.Services;
using OpenSSH_GUI.Dialogs.Enums;
using OpenSSH_GUI.Dialogs.Interfaces;
using OpenSSH_GUI.Resources;
using OpenSSH_GUI.Views;
using ReactiveUI;
using Renci.SshNet;
using SshNet.Keygen;
using SshNet.Keygen.Extensions;

namespace OpenSSH_GUI.ViewModels;
[UsedImplicitly]
public class MainWindowViewModel : ViewModelBase<MainWindowViewModel>
{
    private static readonly string? ProjectUrl = Assembly.GetExecutingAssembly()
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .FirstOrDefault(a => a.Key == "ProjectUrl")?.Value;

    private readonly IDisposable _keyCountObservable;
    private readonly IDialogHost _dialogHost;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMessageBoxProvider _messageBoxProvider;

    public MainWindowViewModel(
        ILogger<MainWindowViewModel> logger,
        SshKeyManager sshKeyManager,
        IServerConnectionService serverConnectionService,
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        IMessageBoxProvider messageBoxProvider,
        IDialogHost dialogHost) : base(logger)
    {
        SshKeyManager = sshKeyManager;
        ServerConnectionService = serverConnectionService;
        _serviceProvider = serviceProvider;
        _messageBoxProvider = messageBoxProvider;
        _dialogHost = dialogHost;
        _keyCountObservable = sshKeyManager.ObservableForProperty(manager => manager.SshKeysCount).Subscribe(OnKeyCountChanged);

        DisconnectServer = ReactiveCommand.CreateFromTask(DisconnectFromServerAsync);
        ProvidePassword = ReactiveCommand.CreateFromTask<SshKeyFile>(ProvidePasswordAsync);
        NotImplementedMessage = ReactiveCommand.CreateFromTask(ShowNotImplementedMessageBoxAsync);
        OpenBrowser = ReactiveCommand.CreateFromTask<int>(OpenBrowserAsync);
        OpenExportKeyWindowPublic = ReactiveCommand.CreateFromTask<SshKeyFile>(ShowPublicKeyExportWindow);
        OpenExportKeyWindowPrivate = ReactiveCommand.CreateFromTask<SshKeyFile>(ShowPrivateKeyExportWindow);
        OpenConnectToServerWindow =
            ReactiveCommand.CreateFromTask(OpenWindow<ConnectToServerWindow, ConnectToServerViewModel>);
        OpenEditKnownHostsWindow =
            ReactiveCommand.CreateFromTask(OpenWindow<EditKnownHostsWindow, EditKnownHostsWindowViewModel>);
        OpenEditAuthorizedKeysWindow =
            ReactiveCommand.CreateFromTask(OpenWindow<EditAuthorizedKeysWindow, EditAuthorizedKeysViewModel>);
        OpenCreateKeyWindow = ReactiveCommand.CreateFromTask(OpenWindow<AddKeyWindow, AddKeyWindowViewModel>);
        DeleteKey = ReactiveCommand.CreateFromTask<SshKeyFile>(DeleteKeyAsync);
        ReloadKeys = ReactiveCommand.CreateFromTask(SshKeyManager.RerunSearchAsync);
        ShowPassword = ReactiveCommand.CreateFromTask<SshKeyFile>(ShowPasswordExportWindow);

        Version = configuration["RUNNING_VERSION"] ?? "VERSION ERROR";
        
    }

    public ReactiveCommand<Unit, Unit> DisconnectServer { get; }
    public ReactiveCommand<SshKeyFile, Unit> ProvidePassword { get; }
    public ReactiveCommand<Unit, Unit> NotImplementedMessage { get; }
    public ReactiveCommand<int, Unit> OpenBrowser { get; }
    public ReactiveCommand<SshKeyFile, Unit> OpenExportKeyWindowPublic { get; }
    public ReactiveCommand<SshKeyFile, Unit> OpenExportKeyWindowPrivate { get; }
    public ReactiveCommand<Unit, Unit> OpenConnectToServerWindow { get; }
    public ReactiveCommand<Unit, Unit> OpenEditKnownHostsWindow { get; }
    public ReactiveCommand<Unit, Unit> OpenEditAuthorizedKeysWindow { get; }
    public ReactiveCommand<Unit, Unit> OpenCreateKeyWindow { get; }
    public ReactiveCommand<SshKeyFile, Unit> DeleteKey { get; }
    public ReactiveCommand<Unit, Unit> ReloadKeys { get; }
    public ReactiveCommand<SshKeyFile, Unit> ShowPassword { get; }


    public IServerConnectionService ServerConnectionService { get; }
    public SshKeyManager SshKeyManager { get; }


    public string WindowTitle => string.Format(StringsAndTexts.MainWindowTitle, Version);

    public string Version
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public MaterialIcon ItemsCountIcon
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = new()
    {
        Kind = MaterialIconKind.ErrorOutline,
        Width = 20,
        Height = 20
    };


    public bool? KeyTypeSort
    {
        get;
        set
        {
            KeyTypeSortDirectionIcon = EvaluateSortIconKind(value);
            SshKeyManager.ChangeOrder(value switch
            {
                null => key => key.OrderBy(e => e.FileName),
                true => key => key.OrderBy(e => e.KeyType),
                false => key => key.OrderByDescending(e => e.KeyType)
            });
            this.RaiseAndSetIfChanged(ref field, value);
        }
    }

    public MaterialIconKind KeyTypeSortDirectionIcon
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = MaterialIconKind.CircleOutline;

    public bool? CommentSort
    {
        get;
        set
        {
            CommentSortDirectionIcon = EvaluateSortIconKind(value);
            SshKeyManager.ChangeOrder(value switch
            {
                null => key => key.OrderBy(e => e.FileName),
                true => key => key.OrderBy(e => e.Comment),
                false => key => key.OrderByDescending(e => e.Comment)
            });
            this.RaiseAndSetIfChanged(ref field, value);
        }
    }

    public MaterialIconKind CommentSortDirectionIcon
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = MaterialIconKind.CircleOutline;

    public bool? FingerPrintSort
    {
        get;
        set
        {
            FingerPrintSortDirectionIcon = EvaluateSortIconKind(value);
            SshKeyManager.ChangeOrder(value switch
            {
                null => key => key.OrderBy(e => e.FileName),
                true => key => key.OrderBy(e => e.Fingerprint),
                false => key => key.OrderByDescending(e => e.Fingerprint)
            });
            this.RaiseAndSetIfChanged(ref field, value);
        }
    }

    public MaterialIconKind FingerPrintSortDirectionIcon
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = MaterialIconKind.CircleOutline;

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
                if(keyFile is not null)
                    content = keyFile.ToOpenSshFormat(passwordProtectedKeyFile.Password.GetPasswordString());
                break;
            }
            default:
            {
                if(keyFile is not null)
                    content = keyFile.ToOpenSshFormat();
                break;
            }
        }
        return ShowExportWindow(key, content, null, token);
    }

    private Task ShowPublicKeyExportWindow(SshKeyFile key, CancellationToken token = default)
    {
        PrivateKeyFile? keyFile = key;
        var content = keyFile is not null ? keyFile.ToOpenSshPublicFormat() : string.Empty;
        return ShowExportWindow(key, content, null, token);
    }

    private Task ShowPasswordExportWindow(SshKeyFile key, CancellationToken token = default)
    {
        if(!key.Password.IsValid) return Task.CompletedTask;
        return ShowExportWindow(key, key.Password.GetPasswordString(),
            string.Format(StringsAndTexts.KeysShowPasswordOf, key.AbsoluteFilePath), token);
    }

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

        var view = await _serviceProvider.ResolveViewAsync<ExportWindow, ExportWindowViewModel>(
            new ExportWindowViewModelInitializerParameters
            {
                Export = content,
                WindowTitle = windowTitle
            }, token: token);
        await _dialogHost.ShowDialog(view);
    }

    private async Task ShowNotImplementedMessageBoxAsync(CancellationToken cancellationToken = default)
    {
        await _messageBoxProvider.ShowMessageBoxAsync(StringsAndTexts.NotImplementedBoxTitle,
            StringsAndTexts.NotImplementedBoxText, MessageBoxButtons.Ok, MessageBoxIcon.Information);
    }

    private static async Task OpenBrowserAsync(int commandTypeParameter, CancellationToken cancellationToken = default)
    {
        var url = commandTypeParameter switch
        {
            1 => string.Join("/", ProjectUrl, "issues"),
            2 => string.Join("#", ProjectUrl, "authors"),
            _ => ProjectUrl
        };


        var processStartInfo = new ProcessStartInfo
        {
            Arguments = url
        };

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            processStartInfo.FileName = "cmd";
            processStartInfo.Arguments = string.Empty;
            processStartInfo.ArgumentList.Add("/c");
            processStartInfo.ArgumentList.Add("start");
            processStartInfo.ArgumentList.Add(url?.Replace("&", "^&") ?? string.Empty);
            processStartInfo.CreateNoWindow = true;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            processStartInfo.FileName = "open";
        }
        else
        {
            processStartInfo.FileName = "xdg-open";
        }

        if (Process.Start(processStartInfo) is { } process)
            await process.WaitForExitAsync(cancellationToken);
    }


    private async Task DisconnectFromServerAsync(CancellationToken cancellationToken)
    {
        var messageBoxText = StringsAndTexts.MainWindowDisconnectBoxTextSuccess;
        var messageBoxIcon = MessageBoxIcon.Information;
        if (ServerConnectionService.IsConnected)
        {
            try
            {
                await ServerConnectionService.CloseConnection(cancellationToken);
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

    private async Task OpenWindow<TWindow, TViewModel>(CancellationToken token = default)
        where TWindow : WindowBase<TViewModel> where TViewModel : ViewModelBase<TViewModel>
    {
        await _dialogHost.ShowDialog<TWindow, TViewModel>(
            await _serviceProvider.ResolveViewAsync<TWindow, TViewModel>(token: token));
    }


    private async Task DeleteKeyAsync(SshKeyFile sshKeyFile, CancellationToken cancellationToken = default)
    {
        if (await _messageBoxProvider.ShowMessageBoxAsync(
                string.Format(StringsAndTexts.MainWindowViewModelDeleteKeyTitleText, sshKeyFile.FileName),
                StringsAndTexts.MainWindowViewModelDeleteKeyQuestionTextPair, MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) is MessageBoxResult.Yes)
            sshKeyFile.Delete();
    }

    private async Task ProvidePasswordAsync(SshKeyFile key, CancellationToken cancellationToken = default)
    {
        var trys = 0;

        while (key.NeedsPassword && trys < 3)
        {
            using var secureInputResult = await _messageBoxProvider.ShowSecureInputAsync(StringsAndTexts.MainWindowViewModelProvidePasswordPromptHeading,
                string.Format(StringsAndTexts.MainWindowViewModelProvidePasswordPromptBodyHeading,
                    Path.GetFileName(key.AbsoluteFilePath)));
            if(secureInputResult != null && await key.SetPassword(secureInputResult.Value))
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


    private static MaterialIconKind EvaluateSortIconKind(bool? value) =>
        value switch
        {
            null => MaterialIconKind.CircleOutline,
            true => MaterialIconKind.ChevronDownCircleOutline,
            false => MaterialIconKind.ChevronUpCircleOutline
        };

    private void OnKeyCountChanged(IObservedChange<SshKeyManager, int> obj)
    {
        ItemsCountIcon = new MaterialIcon
        {
            Kind = obj.Value switch
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
    }
}