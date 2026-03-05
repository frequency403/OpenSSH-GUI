#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:46

#endregion

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Material.Icons;
using Material.Icons.Avalonia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia.Models;
using OpenSSH_GUI.Core.Database.Context;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Interfaces.Keys;
using OpenSSH_GUI.Core.Interfaces.Misc;
using OpenSSH_GUI.Core.Lib.Misc;
using OpenSSH_GUI.Core.Lib.Static;
using OpenSSH_GUI.Core.MVVM;
using OpenSSH_GUI.Core.Services;
using ReactiveUI;
using SshNet.Keygen;

namespace OpenSSH_GUI.ViewModels;

public class MainWindowViewModel : ViewModelBase<MainWindowViewModel>
{
    public readonly Interaction<ApplicationSettingsViewModel, ApplicationSettingsViewModel?> ShowAppSettings = new();
    public readonly Interaction<ConnectToServerViewModel, ConnectToServerViewModel?> ShowConnectToServerWindow = new();
    public readonly Interaction<AddKeyWindowViewModel, AddKeyWindowViewModel?> ShowCreate = new();

    public readonly Interaction<EditAuthorizedKeysViewModel, EditAuthorizedKeysViewModel?> ShowEditAuthorizedKeys =
        new();

    public readonly Interaction<EditKnownHostsWindowViewModel, EditKnownHostsWindowViewModel?> ShowEditKnownHosts = new();
    public readonly Interaction<ExportWindowViewModel, ExportWindowViewModel?> ShowExportWindow = new();

    private IServerConnection _serverConnection;

    private ObservableCollection<SshKeyFile> _sshKeys = [];

    public string Version
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    private readonly DirectoryCrawler _directoryCrawler;
    private readonly IServiceProvider _serviceProvider;
    private readonly KeyLocatorService _keyLocatorService;
    
    public MainWindowViewModel(ILogger<MainWindowViewModel> logger, DirectoryCrawler directoryCrawler, KeyLocatorService locatorService, IServiceProvider serviceProvider) : base(logger)
    {
        _directoryCrawler = directoryCrawler;
        _serviceProvider = serviceProvider;
        _keyLocatorService = locatorService;
        foreach (var sshKeyFile in directoryCrawler.GetNewFromDisk())
        {
            _sshKeys.Add(sshKeyFile);
        }
        _serverConnection = new ServerConnection("123", "123", "123");
        EvaluateAppropriateIcon();
        _sshKeys.CollectionChanged += (sender, args) => EvaluateAppropriateIcon();
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        Version = version is null ? "0.0.0" : string.Format(StringsAndTexts.Version, version?.Major, version?.Minor, version?.Build);
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
        if (ServerConnection.IsConnected)
        {
            if (!ServerConnection.CloseConnection(out var exception))
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
            var connectToServer = _serviceProvider.GetService<ConnectToServerViewModel>();
            var windowResult = await ShowConnectToServerWindow.Handle(connectToServer);
            if (windowResult is not null) ServerConnection = windowResult.ServerConnection;
            return windowResult;
        });

    public ReactiveCommand<Unit, EditKnownHostsWindowViewModel?> OpenEditKnownHostsWindow =>
        ReactiveCommand.CreateFromTask<Unit, EditKnownHostsWindowViewModel?>(async e =>
        {
            var editKnownHosts = _serviceProvider.GetService<EditKnownHostsWindowViewModel>();
            editKnownHosts.SetServerConnection(ref _serverConnection);
            return await ShowEditKnownHosts.Handle(editKnownHosts);
        });

    public ReactiveCommand<ISshKey, ExportWindowViewModel?> OpenExportKeyWindowPublic =>
        ReactiveCommand.CreateFromTask<ISshKey, ExportWindowViewModel?>(async key =>
            await ShowExportWindowWithText(key, true));

    public ReactiveCommand<ISshKey, ExportWindowViewModel?> OpenExportKeyWindowPrivate =>
        ReactiveCommand.CreateFromTask<ISshKey, ExportWindowViewModel?>(async key =>
            await ShowExportWindowWithText(key, false));

    public ReactiveCommand<Unit, EditAuthorizedKeysViewModel?> OpenEditAuthorizedKeysWindow =>
        ReactiveCommand.CreateFromTask<Unit, EditAuthorizedKeysViewModel?>(
            async e =>
            {
                try
                {
                    var editAuthorizedKeysViewModel = _serviceProvider.GetService<EditAuthorizedKeysViewModel>();
                    //editAuthorizedKeysViewModel.SetConnectionAndKeys(ref _serverConnection, ref _sshKeys);
                    return await ShowEditAuthorizedKeys.Handle(editAuthorizedKeysViewModel);
                }
                catch (Exception exception)
                {
                    var messageBox = MessageBoxManager.GetMessageBoxStandard(StringsAndTexts.Error, exception.Message,
                        ButtonEnum.Ok, Icon.Error);
                    await messageBox.ShowAsync();
                    return null;
                }
            });

    public ReactiveCommand<Unit, AddKeyWindowViewModel?> OpenCreateKeyWindow =>
        ReactiveCommand.CreateFromTask<Unit, AddKeyWindowViewModel?>(async e =>
        {
            var create = new AddKeyWindowViewModel();
            var result = await ShowCreate.Handle(create);
            if (result == null) return result;
            Exception? exception = null;
            try
            {
                var newKey = await result.RunKeyGen();
                if (newKey != null) SshKeys.Add(newKey);
            }
            catch (Exception e1)
            {
                exception = e1;
            }

            if (exception is null) return result;
            var msgBox = MessageBoxManager.GetMessageBoxStandard(StringsAndTexts.Error, exception.Message,
                ButtonEnum.Ok, Icon.Error);
            await msgBox.ShowAsync();
            return result;
        });

    public ReactiveCommand<ISshKey, ISshKey?> DeleteKey =>
        ReactiveCommand.CreateFromTask<ISshKey, ISshKey?>(async u =>
        {
            var box = MessageBoxManager.GetMessageBoxStandard(
                string.Format(StringsAndTexts.MainWindowViewModelDeleteKeyTitleText, u.Filename),
                u is ISshPublicKey
                    ? StringsAndTexts.MainWindowViewModelDeleteKeyQuestionTextPair
                    : StringsAndTexts.MainWindowViewModelDeleteKeyQuestionText, ButtonEnum.YesNo, Icon.Question);
            var res = await box.ShowAsync();
            if (res != ButtonResult.Yes) return null;
            u.DeleteKey();
            SshKeys.Remove(u);
            await using var context = new OpenSshGuiDbContext();
            context.KeyDtos.Remove(await context.KeyDtos.FirstAsync(e => e.AbsolutePath == u.AbsoluteFilePath));
            await context.SaveChangesAsync();
            return u;
        });

    public ReactiveCommand<ISshKey, ISshKey?> ConvertKey => ReactiveCommand.CreateFromTask<ISshKey, ISshKey?>(
        async key =>
        {
            var currentFormat = Enum.GetName(key.Format);
            var oppositeFormat =
                Enum.GetName(key.Format is SshKeyFormat.OpenSSH ? SshKeyFormat.PuTTYv3 : SshKeyFormat.OpenSSH);

            var title = string.Format(StringsAndTexts.MainWindowConvertKeyMessageBoxTitle, currentFormat,
                oppositeFormat);
            var text = string.Format(StringsAndTexts.MainWindowConvertKeyMessageBoxText, title);

            var box = MessageBoxManager.GetMessageBoxStandard(title, text, ButtonEnum.YesNoAbort, Icon.Warning);
            var errorBox = MessageBoxManager.GetMessageBoxStandard(
                string.Format(StringsAndTexts.ErrorAction,
                    string.Format(StringsAndTexts.MainWindowConvertKeyMessageBoxTitle, currentFormat, oppositeFormat)),
                StringsAndTexts.MainWindowConvertKeyMessageBoxErrorText, ButtonEnum.Ok, Icon.Error);
            var oldIndex = SshKeys.IndexOf(key);
            var result = await box.ShowAsync();
            if (result is ButtonResult.Abort or ButtonResult.None) return key;

            var formatted = await KeyFactory.ConvertToOppositeFormatAsync(key);
            if (formatted is null)
            {
                await errorBox.ShowAsync();
                return key;
            }

            SshKeys.Remove(key);
            SshKeys.Insert(oldIndex, formatted);
            if (result == ButtonResult.Yes) key.DeleteKey();
            return key;
        });

    public ReactiveCommand<bool, bool> ReloadKeys => ReactiveCommand.CreateFromTask<bool, bool>(async input =>
    {
        SshKeys.Clear();
        await foreach (var key in _directoryCrawler.GetAllKeysYield(true, input)) SshKeys.Add(key);
        return input;
    });

    public ReactiveCommand<ISshKey, ISshKey> ShowPassword => ReactiveCommand.CreateFromTask<ISshKey, ISshKey>(
        async key =>
        {
            var exportView = new ExportWindowViewModel
            {
                Export = key.Password,
                WindowTitle = string.Format(StringsAndTexts.KeysShowPasswordOf, key.AbsoluteFilePath)
            };
            await ShowExportWindow.Handle(exportView);
            return key;
        });

    public ReactiveCommand<ISshKey, ISshKey> ForgetPassword => ReactiveCommand.CreateFromTask<ISshKey, ISshKey>(
        async key =>
        {
            await using var context = new OpenSshGuiDbContext();
            var dto = await context.KeyDtos.FirstAsync(e => e.AbsolutePath == key.AbsoluteFilePath);
            dto.Password = "";
            await context.SaveChangesAsync();
            var index = SshKeys.IndexOf(key);
            var keyInList = KeyFactory.FromDtoId(dto.Id);
            SshKeys.RemoveAt(index);
            SshKeys.Insert(index, keyInList);
            return keyInList;
        });

    public ReactiveCommand<Unit, ApplicationSettingsViewModel> OpenAppSettings =>
        ReactiveCommand.CreateFromTask<Unit, ApplicationSettingsViewModel>(async u =>
        {
            var vm = _serviceProvider.GetRequiredService<ApplicationSettingsViewModel>();
            var result = await ShowAppSettings.Handle(vm);
            return result;
        });

    public ReactiveCommand<ISshKey, ISshKey?> ProvidePassword => ReactiveCommand.CreateFromTask<ISshKey, ISshKey?>(
        async key =>
        {
            var trys = 0;

            while (key.NeedPassword && trys < 3)
            {
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
                    WindowIcon = App.WindowIcon,
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
                var sshKey = await KeyFactory.ProvidePasswordForKeyAsnyc(key, passwordDialog.InputValue);
                if (sshKey.NeedPassword)
                {
                    var msgBox = MessageBoxManager.GetMessageBoxStandard(new MessageBoxStandardParams
                    {
                        ContentTitle = StringsAndTexts.MainWindowViewModelProvidePasswordErrorHeading,
                        ContentMessage = string.Format(StringsAndTexts.MainWindowViewModelProvidePasswordErrorContent,
                            trys + 1, 3),
                        Icon = Icon.Warning,
                        WindowIcon = App.WindowIcon,
                        ButtonDefinitions = ButtonEnum.OkAbort,
                        EnterDefaultButton = ClickEnum.Ok,
                        EscDefaultButton = ClickEnum.Abort
                    });
                    var res = await msgBox.ShowAsync();
                    if (res is ButtonResult.Abort) return null;
                    trys++;
                    continue;
                }

                await UpdateKeyInDatabase(sshKey);

                var index = SshKeys.IndexOf(key);
                SshKeys.RemoveAt(index);
                SshKeys.Insert(index, sshKey);
                break;
            }

            return key;
        });

    public IServerConnection ServerConnection
    {
        get => _serverConnection;
        private set => this.RaiseAndSetIfChanged(ref _serverConnection, value);
    }

    public ObservableCollection<ISshKey?> SshKeys { get; set; } = new ObservableCollection<ISshKey?>();
    // {
    //     get => _sshKeys;
    //     set => this.RaiseAndSetIfChanged(ref _sshKeys, value);
    // }

    public bool? KeyTypeSort
    {
        get;
        set
        {
            KeyTypeSortDirectionIcon = EvaluateSortIconKind(value);
            SshKeys = new ObservableCollection<ISshKey>(value switch
            {
                null => SshKeys.OrderBy(e => e.Id),
                true => SshKeys.OrderBy(e => e.KeyType.BaseType),
                false => SshKeys.OrderByDescending(e => e.KeyType.BaseType)
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
            SshKeys = new ObservableCollection<ISshKey>(value switch
            {
                null => SshKeys.OrderBy(e => e.Id),
                true => SshKeys.OrderBy(e => e.Comment),
                false => SshKeys.OrderByDescending(e => e.Comment)
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
            SshKeys = new ObservableCollection<ISshKey>(value switch
            {
                null => SshKeys.OrderBy(e => e.Id),
                true => SshKeys.OrderBy(e => e.Fingerprint),
                false => SshKeys.OrderByDescending(e => e.Fingerprint)
            });
            this.RaiseAndSetIfChanged(ref field, value);
        }
    }

    public MaterialIconKind FingerPrintSortDirectionIcon
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = MaterialIconKind.CircleOutline;

    private async Task UpdateKeyInDatabase(ISshKey key)
    {
        await using var context = new OpenSshGuiDbContext();
        var found = await context.KeyDtos.Where(e => e.AbsolutePath == key.AbsoluteFilePath).FirstOrDefaultAsync();
        if (found is null)
        {
            context.KeyDtos.Add(key.ToDto());
        }
        else
        {
            var keyDto = key.ToDto();
            found.AbsolutePath = keyDto.AbsolutePath;
            found.Format = keyDto.Format;
            found.Password = keyDto.Password;
        }

        await context.SaveChangesAsync();
    }

    private async Task<ExportWindowViewModel?> ShowExportWindowWithText(ISshKey key, bool @public)
    {
        var keyExport = @public ? key.ExportOpenSshPublicKey() : key.ExportOpenSshPrivateKey();
        if (keyExport is null)
        {
            var alert = MessageBoxManager.GetMessageBoxStandard(StringsAndTexts.Error,
                StringsAndTexts.MainWindowViewModelExportKeyErrorMessage,
                ButtonEnum.Ok, Icon.Error);
            await alert.ShowAsync();
            return null;
        }

        var exportViewModel = new ExportWindowViewModel();
        exportViewModel.Export = keyExport;
        exportViewModel.WindowTitle = string.Format(StringsAndTexts.MainWindowViewModelDynamicExportWindowTitle,
            Enum.GetName(key.KeyType.BaseType), key.Filename);
        return await ShowExportWindow.Handle(exportViewModel);
    }


    private void EvaluateAppropriateIcon()
    {
        ItemsCount = new MaterialIcon
        {
            Kind = SshKeys.Count switch
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