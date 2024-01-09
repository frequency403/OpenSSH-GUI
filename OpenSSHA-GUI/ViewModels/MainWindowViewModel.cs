using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using OpenSSHALib.Lib;
using OpenSSHALib.Models;
using ReactiveUI;

namespace OpenSSHA_GUI.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public readonly Interaction<AddKeyWindowViewModel, AddKeyWindowViewModel?> ShowCreate = new();
    public readonly Interaction<EditKnownHostsViewModel, EditKnownHostsViewModel?> ShowEditKnownHosts = new();
    public readonly Interaction<ExportWindowViewModel, ExportWindowViewModel?> ShowExportWindow = new();
    public readonly Interaction<UploadToServerViewModel, UploadToServerViewModel?> ShowUploadToServer = new();
    private ObservableCollection<SshPublicKey> _sshKeys = new(DirectoryCrawler.GetAllKeys());

    public ReactiveCommand<Unit, EditKnownHostsViewModel?> OpenEditKnownHostsWindow =>
        ReactiveCommand.CreateFromTask<Unit, EditKnownHostsViewModel?>(async e =>
        {
            var editKnownHosts = new EditKnownHostsViewModel();
            return await ShowEditKnownHosts.Handle(editKnownHosts);
        });

    public ReactiveCommand<SshKey, ExportWindowViewModel?> OpenExportKeyWindow =>
        ReactiveCommand.CreateFromTask<SshKey, ExportWindowViewModel?>(async key =>
        {
            var keyExport = await key.ExportKey();
            if (keyExport is null)
            {
                var alert = MessageBoxManager.GetMessageBoxStandard(StringsAndTexts.Error,
                    StringsAndTexts.MainWindowViewModelExportKeyErrorMessage,
                    ButtonEnum.Ok, Icon.Error);
                await alert.ShowAsync();
                return null;
            }

            var exportViewModel = new ExportWindowViewModel
            {
                WindowTitle = string.Format(StringsAndTexts.MainWindowViewModelDynamicExportWindowTitle,
                    key.KeyTypeString, key.Fingerprint),
                Export = keyExport
            };
            return await ShowExportWindow.Handle(exportViewModel);
        });


    public ReactiveCommand<Unit, UploadToServerViewModel?> OpenUploadToServerWindow => ReactiveCommand.CreateFromTask<Unit, UploadToServerViewModel?>(async e =>
    {
        var uploadViewModel = new UploadToServerViewModel(_sshKeys);
        var result = await ShowUploadToServer.Handle(uploadViewModel);
        return result;
    });
    
    public ReactiveCommand<Unit, AddKeyWindowViewModel?> OpenCreateKeyWindow =>
        ReactiveCommand.CreateFromTask<Unit, AddKeyWindowViewModel?>(async e =>
        {
            var create = new AddKeyWindowViewModel();
            var result = await ShowCreate.Handle(create);
            if (result == null) return result;
            var newKey = await result.RunKeyGen();
            if (newKey != null) SshKeys.Add(newKey);
            return result;
        });

    public ReactiveCommand<SshPublicKey, SshPublicKey?> DeleteKey =>
        ReactiveCommand.CreateFromTask<SshPublicKey, SshPublicKey?>(async u =>
        {
            var box = MessageBoxManager.GetMessageBoxStandard(
                string.Format(StringsAndTexts.MainWindowViewModelDeleteKeyTitleText, u.Filename, u.PrivateKey.Filename),
                StringsAndTexts.MainWindowViewModelDeleteKeyQuestionText, ButtonEnum.YesNo, Icon.Question);
            var res = await box.ShowAsync();
            if (res != ButtonResult.Yes) return null;
            u.DeleteKey();
            SshKeys.Remove(u);
            return u;
        });

    public ObservableCollection<SshPublicKey> SshKeys
    {
        get => _sshKeys;
        set => this.RaiseAndSetIfChanged(ref _sshKeys, value);
    }
}