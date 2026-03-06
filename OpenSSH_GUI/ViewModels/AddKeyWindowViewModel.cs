#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:42

#endregion

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive;
using Microsoft.Extensions.Logging;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Interfaces.Keys;
using OpenSSH_GUI.Core.Lib.Misc;
using OpenSSH_GUI.Core.Lib.Static;
using OpenSSH_GUI.Core.MVVM;
using OpenSSH_GUI.Core.Services;
using ReactiveUI;
using ReactiveUI.Validation.Abstractions;
using ReactiveUI.Validation.Contexts;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Helpers;
using SshNet.Keygen;

namespace OpenSSH_GUI.ViewModels;

public sealed class AddKeyWindowViewModel(ILogger<AddKeyWindowViewModel> logger, KeyLocatorService keyLocatorService) : ViewModelBase<AddKeyWindowViewModel>(logger), IValidatableViewModel
{
    private bool _createKey;
    private ISshKeyType _selectedKeyType;
    private ObservableCollection<ISshKeyType> _sshKeyTypes;

    private async Task<AddKeyWindowViewModel?> OnSubmit(bool createKey)
    {
        _createKey = createKey;
        if (!_createKey)
        {
            RequestClose();
            return null;
        }
        var fullNewFilePath = Path.Combine(SshConfigFilesExtension.GetBaseSshPath(), KeyName);
        if (!File.Exists(fullNewFilePath)) return null;
        try
        {
            await keyLocatorService.GenerateNewKeyInFile(fullNewFilePath, new SshKeyGenerateParams(
                SelectedKeyType.BaseType,
                KeyFormat,
                string.IsNullOrWhiteSpace(KeyName) ? null : KeyName,
                null,
                string.IsNullOrWhiteSpace(Password) ? null : Password,
                string.IsNullOrWhiteSpace(Comment) ? null : Comment,
                SelectedKeyType.CurrentBitSize
            ));
            RequestClose();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error creating key");
            var msgBox = MessageBoxManager.GetMessageBoxStandard(StringsAndTexts.Error, e.Message,
                ButtonEnum.Ok, Icon.Error);
            await msgBox.ShowAsync();
        }
        return null;
    }
    
    public override void Initialize(IInitializerParameters<AddKeyWindowViewModel>? parameters = null)
    {
        KeyNameValidationHelper = this.ValidationRule(
            e => e.KeyName,
            name => name is not null && !File.Exists(Path.Combine(SshConfigFilesExtension.GetBaseSshPath(), name)),
            StringsAndTexts.AddKeyWindowFilenameError
        );
        BooleanSubmit = ReactiveCommand.CreateFromTask<bool, AddKeyWindowViewModel?>(OnSubmit);
        _sshKeyTypes = new ObservableCollection<ISshKeyType>(KeyTypeExtension.GetAvailableKeyTypes());
        _selectedKeyType = _sshKeyTypes.First();
        base.Initialize(parameters);
    }

    public ValidationHelper KeyNameValidationHelper
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    } = new(new ValidationContext());

    public ISshKeyType SelectedKeyType
    {
        get => _selectedKeyType;
        set
        {
            try
            {
                KeyName = $"id_{Enum.GetName(value.BaseType)!.ToLower()}";
                this.RaiseAndSetIfChanged(ref _selectedKeyType, value);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }
    }

    public ObservableCollection<ISshKeyType> SshKeyTypes
    {
        get => _sshKeyTypes;
        set => this.RaiseAndSetIfChanged(ref _sshKeyTypes, value);
    }

    public SshKeyFormat KeyFormat
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = SshKeyFormat.OpenSSH;

    public SshKeyFormat[] SshKeyFormats { get; } = Enum.GetValues<SshKeyFormat>();


    public string KeyName
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = "id_rsa";

    public string Comment { get; set; } = $"{Environment.UserName}@{Environment.MachineName}";
    public string Password { get; set; } = "";

    public IValidationContext ValidationContext { get; } = new ValidationContext();
}