#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:46

#endregion

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Material.Icons;
using Material.Icons.Avalonia;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia.Models;
using OpenSSH_GUI.Core.Database.Context;
using OpenSSH_GUI.Core.Interfaces.Keys;
using OpenSSH_GUI.Core.Interfaces.Misc;
using OpenSSH_GUI.Core.Lib.Misc;
using OpenSSH_GUI.Core.Lib.Static;
using ReactiveUI;
using SshNet.Keygen;

namespace OpenSSH_GUI.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public readonly Interaction<ApplicationSettingsViewModel, ApplicationSettingsViewModel> ShowAppSettings = new();
    public readonly Interaction<ConnectToServerViewModel, ConnectToServerViewModel?> ShowConnectToServerWindow = new();
    public readonly Interaction<AddKeyWindowViewModel, AddKeyWindowViewModel?> ShowCreate = new();

    public readonly Interaction<EditAuthorizedKeysViewModel, EditAuthorizedKeysViewModel?> ShowEditAuthorizedKeys =
        new();

    public readonly Interaction<EditKnownHostsViewModel, EditKnownHostsViewModel?> ShowEditKnownHosts = new();
    public readonly Interaction<ExportWindowViewModel, ExportWindowViewModel?> ShowExportWindow = new();
    public readonly Interaction<ConnectionViewModel, ConnectionViewModel?> ShowConnectionWindow = new();

    private MaterialIcon _itemsCount = new()
    {
        Kind = MaterialIconKind.NumericZero,
        Width = 20,
        Height = 20
    };

    public bool KeyContextMenuEnabled { get; } = true;
    private IServerConnection _serverConnection;

    private ObservableCollection<ISshKey?> _sshKeys;
    private OpenSshGuiDbContext _context;

    private async Task UpdateKeyInDatabase(ISshKey key)
    {
        var found = await _context.KeyDtos.Where(e => e.AbsolutePath == key.AbsoluteFilePath).FirstOrDefaultAsync();
        if (found is null)
        {
            _context.KeyDtos.Add(key.ToDto());
        }
        else
        {
            var keyDto = key.ToDto();
            found.AbsolutePath = keyDto.AbsolutePath;
            found.Format = keyDto.Format;
            found.Password = keyDto.Password;
        }
        await _context.SaveChangesAsync();
    }

    public MainWindowViewModel(ILogger<MainWindowViewModel> logger, OpenSshGuiDbContext context) : base(logger)
    {
        _context = context;
        _sshKeys = new ObservableCollection<ISshKey?>(DirectoryCrawler.GetAllKeys());
        _serverConnection = new ServerConnection("123", "123", "123");
        EvaluateAppropriateIcon();
    }

    public MaterialIcon ItemsCount
    {
        get => _itemsCount;
        set => this.RaiseAndSetIfChanged(ref _itemsCount, value);
    }

    public ReactiveCommand<Unit, Unit> NotImplementedMessage => ReactiveCommand.CreateFromTask<Unit, Unit>(async e =>
    {
        var msgBox = MessageBoxManager.GetMessageBoxStandard(StringsAndTexts.NotImplementedBoxTitle,
            StringsAndTexts.NotImplementedBoxText, ButtonEnum.Ok, Icon.Info);
        await msgBox.ShowAsync();
        return e;
    });

    public ReactiveCommand<int, Unit?> OpenBrowser => ReactiveCommand.Create<int, Unit?>(e =>
    {
        var url = e switch
        {
            1 => "https://github.com/frequency403/OpenSSH-GUI/issues",
            2 => "https://github.com/frequency403/OpenSSH-GUI#authors",
            _ => "https://github.com/frequency403/OpenSSH-GUI"
        };

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            url = url.Replace("&", "^&");
            Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Process.Start("xdg-open", url);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Process.Start("open", url);
        }

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

        var msgBox = MessageBoxManager.GetMessageBoxStandard(StringsAndTexts.MainWindowDisconnectBoxTitle, messageBoxText,
            ButtonEnum.Ok, messageBoxIcon);
        await msgBox.ShowAsync();
        return e;
    });

    public ReactiveCommand<Unit, ConnectToServerViewModel?> OpenConnectToServerWindow =>
        ReactiveCommand.CreateFromTask<Unit, ConnectToServerViewModel?>(async e =>
        {
            var connectToServer = App.ServiceProvider.GetRequiredService<ConnectToServerViewModel>();
            connectToServer.SetKeys(ref _sshKeys);
            var windowResult = await ShowConnectToServerWindow.Handle(connectToServer);
            if (windowResult is not null) ServerConnection = windowResult.ServerConnection;
            return windowResult;
        });

    public ReactiveCommand<Unit, ConnectionViewModel?> OpenConnectWindow =>
        ReactiveCommand.CreateFromTask<Unit, ConnectionViewModel?>(async e =>
        {
            var connect = App.ServiceProvider.GetRequiredService<ConnectionViewModel>();
            var res = await ShowConnectionWindow.Handle(connect);
            return res;
        });

    public ReactiveCommand<Unit, EditKnownHostsViewModel?> OpenEditKnownHostsWindow =>
        ReactiveCommand.CreateFromTask<Unit, EditKnownHostsViewModel?>(async e =>
        {
            var editKnownHosts = App.ServiceProvider.GetRequiredService<EditKnownHostsViewModel>();
            editKnownHosts.SetServerConnection(ref _serverConnection);
            return await ShowEditKnownHosts.Handle(editKnownHosts);
        });

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

        var exportViewModel = App.ServiceProvider.GetRequiredService<ExportWindowViewModel>();
        exportViewModel.Export = keyExport;
        exportViewModel.WindowTitle = string.Format(StringsAndTexts.MainWindowViewModelDynamicExportWindowTitle,
            Enum.GetName(key.KeyType.BaseType), key.Filename);
        return await ShowExportWindow.Handle(exportViewModel);
    }

    public ReactiveCommand<ISshKey, ExportWindowViewModel?> OpenExportKeyWindowPublic =>
        ReactiveCommand.CreateFromTask<ISshKey, ExportWindowViewModel?>(async key => await ShowExportWindowWithText(key, true));

    public ReactiveCommand<ISshKey, ExportWindowViewModel?> OpenExportKeyWindowPrivate =>
        ReactiveCommand.CreateFromTask<ISshKey, ExportWindowViewModel?>(async key =>await ShowExportWindowWithText(key, false));

    public ReactiveCommand<Unit, EditAuthorizedKeysViewModel?> OpenEditAuthorizedKeysWindow =>
        ReactiveCommand.CreateFromTask<Unit, EditAuthorizedKeysViewModel?>(
            async e =>
            {
                try
                {
                    var editAuthorizedKeysViewModel =
                        App.ServiceProvider.GetRequiredService<EditAuthorizedKeysViewModel>();
                    editAuthorizedKeysViewModel.SetConnectionAndKeys(ref _serverConnection, ref _sshKeys);
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
            var create = App.ServiceProvider.GetRequiredService<AddKeyWindowViewModel>();
            var result = await ShowCreate.Handle(create);
            if (result == null) return result;
            Exception? exception = null;
            await Task.Run(async () =>
            {
                try
                {
                    var newKey = await result.RunKeyGen();
                    if (newKey != null) SshKeys.Add(newKey);
                }
                catch (Exception e1)
                {
                    exception = e1;
                }
            });
            if (exception is null) return result;
            var msgBox = MessageBoxManager.GetMessageBoxStandard(StringsAndTexts.Error, exception.Message,
                ButtonEnum.Ok, Icon.Error);
            await msgBox.ShowAsync();
            EvaluateAppropriateIcon();
            return result;
        });

    public ReactiveCommand<ISshKey, ISshKey?> DeleteKey =>
        ReactiveCommand.CreateFromTask<ISshKey, ISshKey?>(async u =>
        {
            var box = MessageBoxManager.GetMessageBoxStandard(
                string.Format(StringsAndTexts.MainWindowViewModelDeleteKeyTitleText, u.Filename),
                u is ISshPublicKey ? StringsAndTexts.MainWindowViewModelDeleteKeyQuestionTextPair : StringsAndTexts.MainWindowViewModelDeleteKeyQuestionText, ButtonEnum.YesNo, Icon.Question);
            var res = await box.ShowAsync();
            if (res != ButtonResult.Yes) return null;
            u.DeleteKey();
            SshKeys.Remove(u);
            _context.KeyDtos.Remove(await _context.KeyDtos.FirstAsync(e => e.AbsolutePath == u.AbsoluteFilePath));
            await _context.SaveChangesAsync();
            EvaluateAppropriateIcon();
            return u;
        });

    public ReactiveCommand<ISshKey, ISshKey?> ConvertKey => ReactiveCommand.CreateFromTask<ISshKey, ISshKey?>(
        async key =>
        {
            var currentFormat = Enum.GetName(key.Format);
            var oppositeFormat = Enum.GetName(key.Format is SshKeyFormat.OpenSSH ? SshKeyFormat.PuTTYv3 : SshKeyFormat.OpenSSH);

            var title = string.Format(StringsAndTexts.MainWindowConvertKeyMessageBoxTitle, currentFormat,
                oppositeFormat);
            var text = string.Format(StringsAndTexts.MainWindowConvertKeyMessageBoxText, title);
            
            var box = MessageBoxManager.GetMessageBoxStandard(title, text, ButtonEnum.YesNoAbort, Icon.Warning);
            var errorBox = MessageBoxManager.GetMessageBoxStandard(string.Format(StringsAndTexts.ErrorAction, string.Format(StringsAndTexts.MainWindowConvertKeyMessageBoxTitle, currentFormat, oppositeFormat)),
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
    
    public ReactiveCommand<bool,bool> ReloadKeys => ReactiveCommand.CreateFromTask<bool,bool>(async input =>
    {
        SshKeys.Clear();
        await foreach (var key in DirectoryCrawler.GetAllKeysYield(true, input))
        {
            SshKeys.Add(key);
        }
        return input;
    });
    
    public ReactiveCommand<ISshKey, ISshKey> ShowPassword => ReactiveCommand.CreateFromTask<ISshKey, ISshKey>(async key =>
    {
        var exportView = new ExportWindowViewModel(NullLogger<ExportWindowViewModel>.Instance)
        {
            Export = key.Password,
            WindowTitle = string.Format(StringsAndTexts.KeysShowPasswordOf, key.AbsoluteFilePath)
        };
        await ShowExportWindow.Handle(exportView);
        return key;
    });
    public ReactiveCommand<ISshKey, ISshKey> ForgetPassword => ReactiveCommand.CreateFromTask<ISshKey, ISshKey>(async key =>
    {
        var dto = await _context.KeyDtos.FirstAsync(e => e.AbsolutePath == key.AbsoluteFilePath);
        dto.Password = "";
        await _context.SaveChangesAsync();
        var index = SshKeys.IndexOf(key);
        var keyInList = KeyFactory.FromDtoId(dto.Id);
        SshKeys.RemoveAt(index);
        SshKeys.Insert(index, keyInList);
        return keyInList;
    });
    
    public ReactiveCommand<Unit, ApplicationSettingsViewModel> OpenAppSettings =>
        ReactiveCommand.CreateFromTask<Unit, ApplicationSettingsViewModel>(async u =>
        {
            var vm = App.ServiceProvider.GetRequiredService<ApplicationSettingsViewModel>();
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
                    ContentHeader = string.Format(StringsAndTexts.MainWindowViewModelProvidePasswordPromptBodyHeading, Path.GetFileName(key.AbsoluteFilePath)),
                    // ContentHeader = $"Provide password for key: {Path.GetFileName(key.AbsoluteFilePath)}",
                    InputParams = new InputParams
                    {
                        Label = StringsAndTexts.MainWindowViewModelProvidePasswordPasswordLabel,
                        Multiline = false
                    },
                    Icon = Icon.Question,
                    ButtonDefinitions = [
                        new ButtonDefinition{IsCancel = true, IsDefault = false, Name = StringsAndTexts.MainWindowViewModelProvidePasswordButtonAbort},
                        new ButtonDefinition{IsCancel = false, IsDefault = true, Name = StringsAndTexts.MainWindowViewModelProvidePasswordButtonSubmit}
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
                        ContentMessage = string.Format(StringsAndTexts.MainWindowViewModelProvidePasswordErrorContent, trys+1, 3),
                        Icon = Icon.Warning,
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

    public ObservableCollection<ISshKey?> SshKeys
    {
        get => _sshKeys;
        set => this.RaiseAndSetIfChanged(ref _sshKeys, value);
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
}