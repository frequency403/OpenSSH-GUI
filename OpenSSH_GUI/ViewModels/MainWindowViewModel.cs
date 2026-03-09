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
using OpenSSH_GUI.Core.Services;
using OpenSSH_GUI.Views;
using ReactiveUI;
using SshNet.Keygen;

namespace OpenSSH_GUI.ViewModels;

public class MainWindowViewModel(
    KeyLocatorService locatorService,
    IServerConnectionService serverConnectionService,
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    IDialogHost dialogHost)
    : ViewModelBase<MainWindowViewModel>
{
    private IServerConnection _serverConnection;

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

    public ReactiveCommand<Unit, Unit> NotImplementedMessage => ReactiveCommand.CreateFromTask<Unit, Unit>(async e =>
    {
        var msgBox = MessageBoxManager.GetMessageBoxStandard(StringsAndTexts.NotImplementedBoxTitle,
            StringsAndTexts.NotImplementedBoxText, ButtonEnum.Ok, Icon.Info);
        await msgBox.ShowAsync();
        return e;
    });

    public ReactiveCommand<int, Unit?> OpenBrowser => ReactiveCommand.Create<int, Unit?>(e =>
    {
        var projectUrl = Assembly.GetExecutingAssembly()
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "ProjectUrl")
            ?.Value;
        var url = e switch
        {
            1 => string.Join("/", projectUrl, "issues"),
            2 => string.Join("#", projectUrl, "authors"),
            _ => projectUrl
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

        Process.Start(processStartInfo);

        return null;
    });

    public ReactiveCommand<Unit, Unit> DisconnectServer => ReactiveCommand.CreateFromTask<Unit, Unit>(async e =>
    {
        var messageBoxText = StringsAndTexts.MainWindowDisconnectBoxTextSuccess;
        var messageBoxIcon = Icon.Success;
        if (serverConnectionService.IsConnected)
        {
            try
            {
                await serverConnectionService.CloseConnection();
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
        return e;
    });

    public ReactiveCommand<Unit, ConnectToServerViewModel?> OpenConnectToServerWindow =>
        ReactiveCommand.CreateFromTask<Unit, ConnectToServerViewModel?>(async e =>
        {
            await dialogHost.ShowDialog<ConnectToServerWindow, ConnectToServerViewModel>(
                await serviceProvider.ResolveViewAsync<ConnectToServerWindow, ConnectToServerViewModel>());
            return null;
        });

    public ReactiveCommand<Unit, EditKnownHostsWindowViewModel?> OpenEditKnownHostsWindow =>
        ReactiveCommand.CreateFromTask<Unit, EditKnownHostsWindowViewModel?>(async _ =>
        {
            await dialogHost.ShowDialog(
                await serviceProvider.ResolveViewAsync<EditKnownHostsWindow, EditKnownHostsWindowViewModel>(
                    new EditKnownHostWindowViewModelInitializerParameters
                    {
                        ServerConnection = _serverConnection
                    }));
            return null;
        });

    public ReactiveCommand<SshKeyFile, ExportWindowViewModel?> OpenExportKeyWindowPublic =>
        ReactiveCommand.CreateFromTask<SshKeyFile, ExportWindowViewModel?>(async key =>
            await ShowExportWindowWithText(key, true));

    public ReactiveCommand<SshKeyFile, ExportWindowViewModel?> OpenExportKeyWindowPrivate =>
        ReactiveCommand.CreateFromTask<SshKeyFile, ExportWindowViewModel?>(async key =>
            await ShowExportWindowWithText(key, false));

    public ReactiveCommand<Unit, EditAuthorizedKeysViewModel?> OpenEditAuthorizedKeysWindow =>
        ReactiveCommand.CreateFromTask<Unit, EditAuthorizedKeysViewModel?>(async _ =>
        {
            try
            {
                await dialogHost.ShowDialog(
                    await serviceProvider.ResolveViewAsync<EditAuthorizedKeysWindow, EditAuthorizedKeysViewModel>(
                        new EditAuthorizedKeysViewModelInitializeParameters
                        {
                            ServerConnection = _serverConnection
                        }));
            }
            catch (Exception exception)
            {
                var messageBox = MessageBoxManager.GetMessageBoxStandard(StringsAndTexts.Error, exception.Message,
                    ButtonEnum.Ok, Icon.Error);
                await messageBox.ShowAsync();
            }

            return null;
        });

    public ReactiveCommand<Unit, AddKeyWindowViewModel?> OpenCreateKeyWindow =>
        ReactiveCommand.CreateFromTask<Unit, AddKeyWindowViewModel?>(async _ =>
            await dialogHost.ShowDialog<AddKeyWindow, AddKeyWindowViewModel>(
                await serviceProvider.ResolveViewAsync<AddKeyWindow, AddKeyWindowViewModel>()));

    public ReactiveCommand<SshKeyFile, SshKeyFile?> DeleteKey =>
        ReactiveCommand.CreateFromTask<SshKeyFile, SshKeyFile?>(async u =>
        {
            var box = MessageBoxManager.GetMessageBoxStandard(
                string.Format(StringsAndTexts.MainWindowViewModelDeleteKeyTitleText, u.FileName),
                StringsAndTexts.MainWindowViewModelDeleteKeyQuestionTextPair, ButtonEnum.YesNo, Icon.Question);
            var res = await box.ShowAsync();
            if (res != ButtonResult.Yes) return null;
            //u.DeleteKey();
            LocatorService.SshKeys.Remove(u);
            return u;
        });

    public ReactiveCommand<SshKeyFile, SshKeyFile?> ConvertKey =>
        ReactiveCommand.CreateFromTask<SshKeyFile, SshKeyFile?>(async key =>
        {
            var currentFormat = Enum.GetName(key.SshKeyFormat);
            var oppositeFormat = Enum.GetName(key.SshKeyFormat is SshKeyFormat.OpenSSH
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
            var oldIndex = LocatorService.SshKeys.IndexOf(key);
            var result = await box.ShowAsync();
            if (result is ButtonResult.Abort or ButtonResult.None) return key;

            // var formatted = await KeyFactory.ConvertToOppositeFormatAsync(key);
            // if (formatted is null)
            // {
            //     await errorBox.ShowAsync();
            //     return key;
            // }

            LocatorService.SshKeys.Remove(key);
            // SshKeys.Insert(oldIndex, formatted);
            // if (result == ButtonResult.Yes) key.DeleteKey();
            return key;
        });

    public ReactiveCommand<bool, bool> ReloadKeys => ReactiveCommand.CreateFromTask<bool, bool>(async input =>
    {
        await locatorService.RerunSearchAsync();
        return input;
    });

    public ReactiveCommand<SshKeyFile, SshKeyFile> ShowPassword =>
        ReactiveCommand.CreateFromTask<SshKeyFile, SshKeyFile>(async key =>
        {
            var expView = await serviceProvider.ResolveViewAsync<ExportWindow, ExportWindowViewModel>(
                new ExportWindowViewModelInitializerParameters
                {
                    Export = Encoding.UTF8.GetString(key.Password.Value.Span),
                    WindowTitle = string.Format(StringsAndTexts.KeysShowPasswordOf, key.AbsoluteFilePath)
                });
            await dialogHost.ShowDialog(expView);
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

    public ReactiveCommand<SshKeyFile, SshKeyFile?> ProvidePassword =>
        ReactiveCommand.CreateFromTask<SshKeyFile, SshKeyFile?>(async key =>
        {
            var trys = 0;

            while (key.NeedsPassword && trys < 3)
            {
                var bitmap = new WindowIcon(serviceProvider.GetRequiredKeyedService<Bitmap>("AppIcon"));
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
                if (result != StringsAndTexts.MainWindowViewModelProvidePasswordButtonSubmit) return null;
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
                    if (res is ButtonResult.Abort) return null;
                    trys++;
                    continue;
                }

                var index = LocatorService.SshKeys.IndexOf(key);
                LocatorService.SshKeys.RemoveAt(index);
                LocatorService.SshKeys.Insert(index, key);
                break;
            }

            return key;
        });

    public IServerConnection? ServerConnection => serverConnectionService.ServerConnection;

    public KeyLocatorService LocatorService => locatorService;

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
        _serverConnection = new ServerConnection("123", "123", "123");
        EvaluateAppropriateIcon();
        locatorService.SshKeysCollectionChanged += (sender, args) => EvaluateAppropriateIcon();
        Version = configuration["RUNNING_VERSION"] ?? "VERSION ERROR";
        await base.InitializeAsync(parameters, cancellationToken);
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

        var view = await serviceProvider.ResolveViewAsync<ExportWindow, ExportWindowViewModel>(
            new ExportWindowViewModelInitializerParameters
            {
                Export = keyExport,
                WindowTitle = string.Format(StringsAndTexts.MainWindowViewModelDynamicExportWindowTitle, key.KeyType,
                    key.FileName)
            });
        await dialogHost.ShowDialog(view);
        return null;
    }


    private void EvaluateAppropriateIcon()
    {
        ItemsCount = new MaterialIcon
        {
            Kind = LocatorService.SshKeys.Count switch
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