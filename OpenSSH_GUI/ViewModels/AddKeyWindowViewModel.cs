#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:42

#endregion

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Interfaces.Keys;
using OpenSSH_GUI.Core.Lib.Keys;
using OpenSSH_GUI.Core.Lib.Misc;
using OpenSSH_GUI.Core.Lib.Static;
using ReactiveUI;
using ReactiveUI.Validation.Abstractions;
using ReactiveUI.Validation.Contexts;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Helpers;
using SshNet.Keygen;
using SshNet.Keygen.Extensions;
using SshNet.Keygen.SshKeyEncryption;
using SshKey = SshNet.Keygen.SshKey;
using SshKeyType = SshNet.Keygen.SshKeyType;

namespace OpenSSH_GUI.ViewModels;

public sealed class AddKeyWindowViewModel : ViewModelBase<AddKeyWindowViewModel>, IValidatableViewModel
{
    private readonly ValidationHelper _keyNameValidationHelper = new(new ValidationContext());
    private bool _createKey;

    private SshKeyFormat _keyFormat = SshKeyFormat.OpenSSH;

    private string _keyName = "id_rsa";

    private ISshKeyType _selectedKeyType;

    private ObservableCollection<ISshKeyType> _sshKeyTypes;

    public AddKeyWindowViewModel()
    {
        KeyNameValidationHelper = this.ValidationRule(
            e => e.KeyName,
            name => !File.Exists(Path.Combine(SshConfigFilesExtension.GetBaseSshPath(), name)),
            StringsAndTexts.AddKeyWindowFilenameError
        );
        BooleanSubmit = ReactiveCommand.Create<bool, AddKeyWindowViewModel?>(b =>
        {
            _createKey = b;
            if (!_createKey) return null;
            return !File.Exists(SshConfigFilesExtension.GetBaseSshPath() + Path.DirectorySeparatorChar + KeyName)
                ? this
                : null;
        });
        _sshKeyTypes = new ObservableCollection<ISshKeyType>(KeyTypeExtension.GetAvailableKeyTypes());
        _selectedKeyType = _sshKeyTypes.First();
    }

    public ValidationHelper KeyNameValidationHelper
    {
        get => _keyNameValidationHelper;
        private init => this.RaiseAndSetIfChanged(ref _keyNameValidationHelper, value);
    }
    
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
        get => _keyFormat;
        set => this.RaiseAndSetIfChanged(ref _keyFormat, value);
    }

    public SshKeyFormat[] SshKeyFormats { get; } = Enum.GetValues<SshKeyFormat>();


    public string KeyName
    {
        get => _keyName;
        set => this.RaiseAndSetIfChanged(ref _keyName, value);
    }

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