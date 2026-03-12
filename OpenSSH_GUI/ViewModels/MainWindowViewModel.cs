using System.Diagnostics;
using System.Reactive;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using JetBrains.Annotations;
using Material.Icons;
using Material.Icons.Avalonia;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia.Models;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Interfaces.Hosts;
using OpenSSH_GUI.Core.Interfaces.Services;
using OpenSSH_GUI.Core.Lib.Keys;
using OpenSSH_GUI.Core.MVVM;
using OpenSSH_GUI.Core.Resources.Wrapper;
using OpenSSH_GUI.Views;
using ReactiveUI;
using SshNet.Keygen;

namespace OpenSSH_GUI.ViewModels;
[UsedImplicitly]
public class MainWindowViewModel : ViewModelBase<MainWindowViewModel>
{
    private static readonly string? ProjectUrl = Assembly.GetExecutingAssembly()
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .FirstOrDefault(a => a.Key == "ProjectUrl")?.Value;

    private readonly IDialogHost _dialogHost;
    private readonly IServiceProvider _serviceProvider;

    public MainWindowViewModel(
        ILogger<MainWindowViewModel> logger,
        ISshKeyManager manager,
        IServerConnectionService serverConnectionService,
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        IDialogHost dialogHost) : base(logger)
    {
        Manager = manager;
        ServerConnectionService = serverConnectionService;
        _serviceProvider = serviceProvider;
        _dialogHost = dialogHost;

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
        ReloadKeys = ReactiveCommand.CreateFromTask(Manager.RerunSearchAsync);
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
    public ISshKeyManager Manager { get; }


    public string WindowTitle => string.Format(StringsAndTexts.MainWindowTitle, Version);

    public string Version
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public MaterialIcon ItemsCount =>
        new()
        {
            Kind = Manager.SshKeys.Count switch
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


    public bool? KeyTypeSort
    {
        get;
        set
        {
            KeyTypeSortDirectionIcon = EvaluateSortIconKind(value);
            Manager.ChangeOrder(value switch
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
            Manager.ChangeOrder(value switch
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
            Manager.ChangeOrder(value switch
            {
                null => key => key.OrderBy(e => e.FileName),
                true => key => key.OrderBy(e => e.Fingerprint()),
                false => key => key.OrderByDescending(e => e.Fingerprint())
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
        return ShowExportWindow(key, key.ToOpenSshFormat(), null, token);
    }

    private Task ShowPublicKeyExportWindow(SshKeyFile key, CancellationToken token = default)
    {
        return ShowExportWindow(key, key.ToOpenSshPublicFormat(), null, token);
    }

    private Task ShowPasswordExportWindow(SshKeyFile key, CancellationToken token = default)
    {
        if(key.Password is not { Length: > 0} password) return Task.CompletedTask;
        return ShowExportWindow(key, Encoding.UTF8.GetString(password.Span),
            string.Format(StringsAndTexts.KeysShowPasswordOf, key.AbsoluteFilePath), token);
    }

    private async Task ShowExportWindow(SshKeyFile key, string content, string? windowTitle = null,
        CancellationToken token = default)
    {
        windowTitle ??= string.Format(StringsAndTexts.MainWindowViewModelDynamicExportWindowTitle,
            key.HashAlgorithmName, key.FileName);
        if (string.IsNullOrWhiteSpace(content))
        {
            var alert = MessageBoxManager.GetMessageBoxStandard(StringsAndTexts.Error,
                StringsAndTexts.MainWindowViewModelExportKeyErrorMessage,
                ButtonEnum.Ok, Icon.Error);
            await alert.ShowAsync();
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

    private static async Task ShowNotImplementedMessageBoxAsync(CancellationToken cancellationToken = default)
    {
        var msgBox = MessageBoxManager.GetMessageBoxStandard(StringsAndTexts.NotImplementedBoxTitle,
            StringsAndTexts.NotImplementedBoxText, ButtonEnum.Ok, Icon.Info);
        await msgBox.ShowAsync();
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
        var messageBoxIcon = Icon.Success;
        if (ServerConnectionService.IsConnected)
        {
            try
            {
                await ServerConnectionService.CloseConnection(cancellationToken);
            }
            catch (Exception exception)
            {
                messageBoxText = exception.Message;
                messageBoxIcon = Icon.Error;
            }
        }
        else
        {
            messageBoxText = StringsAndTexts.MainWindowDisconnectBoxTextNone;
            messageBoxIcon = Icon.Error;
        }

        var msgBox = MessageBoxManager.GetMessageBoxStandard(StringsAndTexts.MainWindowDisconnectBoxTitle,
            messageBoxText,
            ButtonEnum.Ok, messageBoxIcon);
        await msgBox.ShowAsync();
    }

    private async Task OpenWindow<TWindow, TViewModel>(CancellationToken token = default)
        where TWindow : WindowBase<TViewModel> where TViewModel : ViewModelBase<TViewModel>
    {
        await _dialogHost.ShowDialog<TWindow, TViewModel>(
            await _serviceProvider.ResolveViewAsync<TWindow, TViewModel>(token: token));
    }


    private static async Task DeleteKeyAsync(SshKeyFile sshKeyFile, CancellationToken cancellationToken = default)
    {
        var box = MessageBoxManager.GetMessageBoxStandard(
            string.Format(StringsAndTexts.MainWindowViewModelDeleteKeyTitleText, sshKeyFile.FileName),
            StringsAndTexts.MainWindowViewModelDeleteKeyQuestionTextPair, ButtonEnum.YesNo, Icon.Question);
        var res = await box.ShowAsync();
        if (res != ButtonResult.Yes)
            return;
        sshKeyFile.Delete();
    }

    private async Task ProvidePasswordAsync(SshKeyFile key, CancellationToken cancellationToken = default)
    {
        var trys = 0;

        while (key.NeedsPassword && trys < 3)
        {
            var bitmap = new WindowIcon(_serviceProvider.GetRequiredKeyedService<Bitmap>("AppIcon"));
            var passwordDialog = MessageBoxManager.GetMessageBoxCustom(new MessageBoxCustomParams
            {
                ContentTitle = StringsAndTexts.MainWindowViewModelProvidePasswordPromptHeading,
                ContentHeader = string.Format(StringsAndTexts.MainWindowViewModelProvidePasswordPromptBodyHeading,
                    Path.GetFileName(key.AbsoluteFilePath)),
                InputParams = new InputParams
                {
                    Label = StringsAndTexts.MainWindowViewModelProvidePasswordPasswordLabel,
                    Multiline = false
                },
                Topmost = true,
                Icon = Icon.Question,
                WindowIcon = bitmap,
                ButtonDefinitions =
                [
                    new ButtonDefinition
                    {
                        IsCancel = true, IsDefault = false,
                        Name = StringsAndTexts.MainWindowViewModelProvidePasswordButtonAbort
                    },
                    new ButtonDefinition
                    {
                        IsCancel = false, IsDefault = true,
                        Name = StringsAndTexts.MainWindowViewModelProvidePasswordButtonSubmit
                    }
                ]
            });
            var result = await passwordDialog.ShowAsync();
            if (result != StringsAndTexts.MainWindowViewModelProvidePasswordButtonSubmit)
                return;
            var setPasswordResult = await key.SetPassword(Encoding.UTF8.GetBytes(result));
            if (!setPasswordResult)
            {
                var msgBox = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
                {
                    ContentTitle = StringsAndTexts.MainWindowViewModelProvidePasswordErrorHeading,
                    ContentMessage = string.Format(StringsAndTexts.MainWindowViewModelProvidePasswordErrorContent,
                        trys + 1, 3),
                    Icon = Icon.Warning,
                    WindowIcon = bitmap,
                    ButtonDefinitions = ButtonEnum.OkAbort,
                    EnterDefaultButton = ClickEnum.Ok,
                    EscDefaultButton = ClickEnum.Abort
                });
                var res = await msgBox.ShowAsync();
                if (res is ButtonResult.Abort)
                    return;
                trys++;
                continue;
            }

            break;
        }
    }


    private static MaterialIconKind EvaluateSortIconKind(bool? value)
    {
        return value switch
        {
            null => MaterialIconKind.CircleOutline,
            true => MaterialIconKind.ChevronDownCircleOutline,
            false => MaterialIconKind.ChevronUpCircleOutline
        };
    }
}