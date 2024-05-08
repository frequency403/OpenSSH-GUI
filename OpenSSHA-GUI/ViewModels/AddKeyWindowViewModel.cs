using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpenSSHALib.Enums;
using OpenSSHALib.Extensions;
using OpenSSHALib.Interfaces;
using OpenSSHALib.Lib.Structs;
using OpenSSHALib.Models;
using ReactiveUI;
using ReactiveUI.Validation.Abstractions;
using ReactiveUI.Validation.Contexts;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Helpers;
using SshNet.Keygen;
using SshNet.Keygen.Extensions;
using SshNet.Keygen.SshKeyEncryption;
using SshKey = SshNet.Keygen.SshKey;

namespace OpenSSHA_GUI.ViewModels;

public class AddKeyWindowViewModel : ViewModelBase, IValidatableViewModel
{
    private bool _createKey;

    private string _keyName = "id_rsa";

    private readonly ValidationHelper _keyNameValidationHelper = new(new ValidationContext());

    private ISshKeyType _selectedKeyType;

    private ObservableCollection<ISshKeyType> _sshKeyTypes;

    public AddKeyWindowViewModel(ILogger<AddKeyWindowViewModel> logger) : base(logger)
    {
        KeyNameValidationHelper = this.ValidationRule(
            e => e.KeyName,
            name => !File.Exists(Path.Combine(SshConfigFilesExtension.GetBaseSshPath(), name)),
            "Filename does already exist!"
        );
        AddKey = ReactiveCommand.Create<string, AddKeyWindowViewModel?>(b =>
        {
            _createKey = bool.Parse(b);
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

    public ReactiveCommand<string, AddKeyWindowViewModel?> AddKey { get; }

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

    private SshKeyFormat _keyFormat = SshKeyFormat.OpenSSH;

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

    public ValidationContext ValidationContext { get; } = new();

    public async ValueTask<ISshKey?> RunKeyGen()
    {
        try
        {
            if (!_createKey) return null;
            var fullFilePath = Path.Combine(SshConfigFilesExtension.GetBaseSshPath(), KeyName);
            var sshKeyGenerateInfo = new SshKeyGenerateInfo
            {
                KeyType = SelectedKeyType.BaseType switch
                {
                    KeyType.RSA => SshNet.Keygen.SshKeyType.RSA,
                    KeyType.ECDSA => SshNet.Keygen.SshKeyType.ECDSA,
                    KeyType.ED25519 => SshNet.Keygen.SshKeyType.ED25519,
                    _ => throw new ArgumentOutOfRangeException()
                },
                Comment = Comment,
                KeyFormat = KeyFormat,
                KeyLength = SelectedKeyType.CurrentBitSize,
                Encryption = !string.IsNullOrWhiteSpace(Password)
                    ? new SshKeyEncryptionAes256(Password)
                    : new SshKeyEncryptionNone()
            };
            await using var privateStream = new MemoryStream();
            var k = SshKey.Generate(privateStream, sshKeyGenerateInfo);
            switch (KeyFormat)
                {
                    case SshKeyFormat.PuTTYv2:
                    case SshKeyFormat.PuTTYv3:
                        await using (var privateStreamWriter = new StreamWriter(File.Create(fullFilePath + ".ppk")))
                        {
                            await privateStreamWriter.WriteAsync(k.ToPuttyFormat());
                        }
                        return new PpkKey(fullFilePath + ".ppk");
                    case SshKeyFormat.OpenSSH:
                    default:
                        await using (var privateStreamWriter = new StreamWriter(File.Create(fullFilePath)))
                        {
                            await privateStreamWriter.WriteAsync(k.ToOpenSshFormat());
                        }
                        await using (var publicStreamWriter = new StreamWriter(File.Create(fullFilePath + ".pub")))
                        {
                            await publicStreamWriter.WriteAsync(k.ToOpenSshPublicFormat());
                        }
                        return new SshPublicKey(fullFilePath + ".pub");
                }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error creating key");
            return null;
        }
    }
}