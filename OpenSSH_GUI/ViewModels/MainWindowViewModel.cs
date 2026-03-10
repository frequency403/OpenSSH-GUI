using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
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
using OpenSSH_GUI.Core.Interfaces;
using OpenSSH_GUI.Core.Interfaces.Misc;
using OpenSSH_GUI.Core.Lib.Keys;
using OpenSSH_GUI.Core.Lib.Misc;
using OpenSSH_GUI.Core.MVVM;
using OpenSSH_GUI.Core.MVVM.Interfaces;
using OpenSSH_GUI.Core.Resources.Wrapper;
using OpenSSH_GUI.Core.Services;
using OpenSSH_GUI.SshConfig;
using OpenSSH_GUI.Views;
using ReactiveUI;
using SshNet.Keygen;

namespace OpenSSH_GUI.ViewModels;

public class MainWindowViewModel : ViewModelBase<MainWindowViewModel>
{
    private static readonly string? ProjectUrl = Assembly.GetExecutingAssembly()
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .FirstOrDefault(a => a.Key == "ProjectUrl")
        ?.Value;
    
    private readonly KeyLocatorService _locatorService;
    private readonly IServerConnectionService _serverConnectionService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly IDialogHost _dialogHost;
    
    public MainWindowViewModel(
        KeyLocatorService locatorService,
        IServerConnectionService serverConnectionService,
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        IDialogHost dialogHost)
    {
        _locatorService = locatorService;
        _serverConnectionService = serverConnectionService;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _dialogHost = dialogHost;
        
        DisconnectServer = ReactiveCommand.CreateFromTask(DisconnectFromServerAsync);
        ProvidePassword = ReactiveCommand.CreateFromTask<SshKeyFile>(ProvidePasswordAsync);
        NotImplementedMessage = ReactiveCommand.CreateFromTask(ShowNotImplementedMessageBoxAsync);
        OpenBrowser = ReactiveCommand.CreateFromTask<int>(OpenBrowserAsync);
        OpenExportKeyWindowPublic = ReactiveCommand.CreateFromTask<SshKeyFile>(ShowPublicKeyExportWindow);
        OpenExportKeyWindowPrivate = ReactiveCommand.CreateFromTask<SshKeyFile>(ShowPrivateKeyExportWindow);
        OpenConnectToServerWindow = ReactiveCommand.CreateFromTask(OpenWindow<ConnectToServerWindow, ConnectToServerViewModel>);
        OpenEditKnownHostsWindow = ReactiveCommand.CreateFromTask(OpenWindow<EditKnownHostsWindow, EditKnownHostsWindowViewModel>);
        OpenEditAuthorizedKeysWindow = ReactiveCommand.CreateFromTask(OpenWindow<EditAuthorizedKeysWindow, EditAuthorizedKeysViewModel>);
        OpenCreateKeyWindow = ReactiveCommand.CreateFromTask(OpenWindow<AddKeyWindow, AddKeyWindowViewModel>);
        DeleteKey = ReactiveCommand.CreateFromTask<SshKeyFile>(DeleteKeyAsync);
        ConvertKey = ReactiveCommand.CreateFromTask<SshKeyFile>(ConvertKeyAsync);
        ReloadKeys = ReactiveCommand.CreateFromTask(_locatorService.RerunSearchAsync);
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
    public ReactiveCommand<SshKeyFile, Unit> ConvertKey { get; }
    public ReactiveCommand<Unit, Unit> ReloadKeys { get; }
    
    

    public string WindowTitle => string.Format(StringsAndTexts.MainWindowTitle, Version);
    
    public string Version
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public bool KeyContextMenuEnabled { get; } = true;

    public MaterialIcon ItemsCount
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = new()
    {
        Kind = MaterialIconKind.NumericZero,
        Width = 20,
        Height = 20
    };

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
            processStartInfo.ArgumentList.Add(url.Replace("&", "^&"));
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
        if (_serverConnectionService.IsConnected)
        {
            try
            {
                await _serverConnectionService.CloseConnection(cancellationToken);
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

    private async Task OpenWindow<TWindow, TViewModel>(CancellationToken token = default) where TWindow : WindowBase<TViewModel> where TViewModel : ViewModelBase<TViewModel>
    {
        await _dialogHost.ShowDialog<TWindow, TViewModel>(
            await _serviceProvider.ResolveViewAsync<TWindow, TViewModel>(token :token));
    }


    private async Task DeleteKeyAsync(SshKeyFile sshKeyFile, CancellationToken cancellationToken = default)
    {
        var box = MessageBoxManager.GetMessageBoxStandard(
            string.Format(StringsAndTexts.MainWindowViewModelDeleteKeyTitleText, sshKeyFile.FileName),
            StringsAndTexts.MainWindowViewModelDeleteKeyQuestionTextPair, ButtonEnum.YesNo, Icon.Question);
        var res = await box.ShowAsync();
        if (res != ButtonResult.Yes) 
            return;
        //u.DeleteKey();
        _locatorService.SshKeys.Remove(sshKeyFile);
    }

    private async Task ConvertKeyAsync(SshKeyFile key, CancellationToken cancellationToken = default)
    {
        var currentFormat = Enum.GetName(key.Format!.Value);
        var oppositeFormat = Enum.GetName(key.Format is SshKeyFormat.OpenSSH
            ? SshKeyFormat.PuTTYv3
            : SshKeyFormat.OpenSSH);

        var title = string.Format(StringsAndTexts.MainWindowConvertKeyMessageBoxTitle, currentFormat,
            oppositeFormat);
        var text = string.Format(StringsAndTexts.MainWindowConvertKeyMessageBoxText, title);

        var box = MessageBoxManager.GetMessageBoxStandard(title, text, ButtonEnum.YesNoAbort, Icon.Warning);
        var errorBox = MessageBoxManager.GetMessageBoxStandard(
            string.Format(StringsAndTexts.ErrorAction,
                string.Format(StringsAndTexts.MainWindowConvertKeyMessageBoxTitle, currentFormat, oppositeFormat)),
            StringsAndTexts.MainWindowConvertKeyMessageBoxErrorText, ButtonEnum.Ok, Icon.Error);
        var oldIndex = _locatorService.SshKeys.IndexOf(key);
        var result = await box.ShowAsync();
        if (result is ButtonResult.Abort or ButtonResult.None) 
            return;

        // var formatted = await KeyFactory.ConvertToOppositeFormatAsync(key);
        // if (formatted is null)
        // {
        //     await errorBox.ShowAsync();
        //     return key;
        // }

        _locatorService.SshKeys.Remove(key);
        // SshKeys.Insert(oldIndex, formatted);
        // if (result == ButtonResult.Yes) key.DeleteKey();
    }

    

    public ReactiveCommand<SshKeyFile, SshKeyFile> ShowPassword =>
        ReactiveCommand.CreateFromTask<SshKeyFile, SshKeyFile>(async key =>
        {
            var expView = await _serviceProvider.ResolveViewAsync<ExportWindow, ExportWindowViewModel>(
                new ExportWindowViewModelInitializerParameters
                {
                    Export = Encoding.UTF8.GetString(key.Password.Value.Span),
                    WindowTitle = string.Format(StringsAndTexts.KeysShowPasswordOf, key.AbsoluteFilePath)
                });
            await _dialogHost.ShowDialog(expView);
            return key;
        });

    public ReactiveCommand<SshKeyFile, SshKeyFile> ForgetPassword =>
        ReactiveCommand.CreateFromTask<SshKeyFile, SshKeyFile>(async key =>
        {
            // var index = SshKeys.IndexOf(key);
            // var keyInList = KeyFactory.FromDtoId(dto.Id);
            // SshKeys.RemoveAt(index);
            // SshKeys.Insert(index, keyInList);
            // return keyInList;
            return null; // @TODO
        });

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

                var index = _locatorService.SshKeys.IndexOf(key);
                _locatorService.SshKeys.RemoveAt(index);
                _locatorService.SshKeys.Insert(index, key);
                break;
            }
    }
    
    
    public IServerConnection? ServerConnection => _serverConnectionService.ServerConnection;

    public KeyLocatorService LocatorService => _locatorService;

    public bool? KeyTypeSort
    {
        get;
        set
        {
            KeyTypeSortDirectionIcon = EvaluateSortIconKind(value);
            // SshKeys = new ObservableCollection<SshKeyFile>(value switch
            // {
            //     null => SshKeys.OrderBy(e => e.Id),
            //     true => SshKeys.OrderBy(e => e.KeyType.BaseType),
            //     false => SshKeys.OrderByDescending(e => e.KeyType.BaseType)
            // }); @TODO
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
            // SshKeys = new ObservableCollection<SshKeyFile>(value switch
            // {
            //     null => SshKeys.OrderBy(e => e.Id),
            //     true => SshKeys.OrderBy(e => e.Comment),
            //     false => SshKeys.OrderByDescending(e => e.Comment)
            // }); @TODO
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
            // SshKeys = new ObservableCollection<SshKeyFile>(value switch
            // {
            //     null => SshKeys.OrderBy(e => e.Id),
            //     true => SshKeys.OrderBy(e => e.Fingerprint),
            //     false => SshKeys.OrderByDescending(e => e.Fingerprint)
            // }); @TODO
            this.RaiseAndSetIfChanged(ref field, value);
        }
    }

    public MaterialIconKind FingerPrintSortDirectionIcon
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = MaterialIconKind.CircleOutline;

    public override async ValueTask InitializeAsync(IInitializerParameters<MainWindowViewModel>? parameters = null, CancellationToken cancellationToken = default)
    {
        EvaluateAppropriateIcon();
        _locatorService.SshKeysCollectionChanged += (sender, args) => EvaluateAppropriateIcon();
        Version = _configuration["RUNNING_VERSION"] ?? "VERSION ERROR";
        await base.InitializeAsync(parameters, cancellationToken);
    }

    private Task ShowPrivateKeyExportWindow(SshKeyFile key, CancellationToken token = default) => ShowExportWindow(key, false, token);
    private Task ShowPublicKeyExportWindow(SshKeyFile key, CancellationToken token = default) => ShowExportWindow(key, true, token);
    private async Task ShowExportWindow(SshKeyFile key, bool showPublicKey, CancellationToken token)
    {
        string? keyExport = null;
        try
        {
            keyExport = showPublicKey ? key.ToOpenSshPublicFormat() : key.ToOpenSshFormat();
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error while exporting key to {publicOrNot} format", showPublicKey ? "public" : "private");
        }

        if (keyExport is null)
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
                Export = keyExport,
                WindowTitle = string.Format(StringsAndTexts.MainWindowViewModelDynamicExportWindowTitle, key.KeyType,
                    key.FileName)
            }, token: token);
        await _dialogHost.ShowDialog(view);
    }
    private async Task<ExportWindowViewModel?> ShowExportWindowWithText(SshKeyFile key, bool @public)
    {
        string? keyExport = null;
        try
        {
            keyExport = @public ? key.ToOpenSshPublicFormat() : key.ToOpenSshFormat();
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error while exporting key to {publicOrNot} format", @public ? "public" : "private");
        }

        if (keyExport is null)
        {
            var alert = MessageBoxManager.GetMessageBoxStandard(StringsAndTexts.Error,
                StringsAndTexts.MainWindowViewModelExportKeyErrorMessage,
                ButtonEnum.Ok, Icon.Error);
            await alert.ShowAsync();
            return null;
        }

        var view = await _serviceProvider.ResolveViewAsync<ExportWindow, ExportWindowViewModel>(
            new ExportWindowViewModelInitializerParameters
            {
                Export = keyExport,
                WindowTitle = string.Format(StringsAndTexts.MainWindowViewModelDynamicExportWindowTitle, key.KeyType,
                    key.FileName)
            });
        await _dialogHost.ShowDialog(view);
        return null;
    }


    private void EvaluateAppropriateIcon()
    {
        ItemsCount = new MaterialIcon
        {
            Kind = _locatorService.SshKeys.Count switch
            {
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

    private MaterialIconKind EvaluateSortIconKind(bool? value)
    {
        return value switch
        {
            null => MaterialIconKind.CircleOutline,
            true => MaterialIconKind.ChevronDownCircleOutline,
            false => MaterialIconKind.ChevronUpCircleOutline
        };
    }
}