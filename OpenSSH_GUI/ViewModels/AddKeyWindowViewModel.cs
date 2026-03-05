#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:42

#endregion

using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Interfaces.Keys;
using OpenSSH_GUI.Core.Lib.Misc;
using OpenSSH_GUI.Core.Lib.Static;
using OpenSSH_GUI.Core.MVVM;
using ReactiveUI;
using ReactiveUI.Validation.Abstractions;
using ReactiveUI.Validation.Contexts;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Helpers;
using SshNet.Keygen;

namespace OpenSSH_GUI.ViewModels;

public sealed class AddKeyWindowViewModel : ViewModelBase<AddKeyWindowViewModel>, IValidatableViewModel
{
    private bool _createKey;

    private ISshKeyType _selectedKeyType;

    private ObservableCollection<ISshKeyType> _sshKeyTypes;

    public AddKeyWindowViewModel()
    {
        KeyNameValidationHelper = this.ValidationRule(
            e => e.KeyName,
            name => !FileOperations.Exists(Path.Combine(SshConfigFilesExtension.GetBaseSshPath(), name)),
            StringsAndTexts.AddKeyWindowFilenameError
        );
        BooleanSubmit = ReactiveCommand.Create<bool, AddKeyWindowViewModel?>(b =>
        {
            _createKey = b;
            if (!_createKey) return null;
            return !FileOperations.Exists(SshConfigFilesExtension.GetBaseSshPath() + Path.DirectorySeparatorChar +
                                          KeyName)
                ? this
                : null;
        });
        _sshKeyTypes = new ObservableCollection<ISshKeyType>(KeyTypeExtension.GetAvailableKeyTypes());
        _selectedKeyType = _sshKeyTypes.First();
    }

    public ValidationHelper KeyNameValidationHelper
    {
        get;
        private init => this.RaiseAndSetIfChanged(ref field, value);
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

    public async ValueTask<ISshKey?> RunKeyGen()
    {
        try
        {
            return await KeyFactory.GenerateNewAsync(new SshKeyGenerateParams(
                SelectedKeyType.BaseType,
                KeyFormat,
                string.IsNullOrWhiteSpace(KeyName) ? null : KeyName,
                null,
                string.IsNullOrWhiteSpace(Password) ? null : Password,
                string.IsNullOrWhiteSpace(Comment) ? null : Comment,
                SelectedKeyType.CurrentBitSize
            ));
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Error creating key");
            return null;
        }
    }
}