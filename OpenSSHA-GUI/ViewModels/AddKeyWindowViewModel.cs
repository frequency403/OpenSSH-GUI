using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using OpenSSHALib.Extensions;
using OpenSSHALib.Models;
using ReactiveUI;
using ReactiveUI.Validation.Abstractions;
using ReactiveUI.Validation.Contexts;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Validation.Helpers;

namespace OpenSSHA_GUI.ViewModels;

public class AddKeyWindowViewModel : ViewModelBase, IValidatableViewModel
{
    private bool _createKey;

    private string _keyName = "id_rsa";

    private SshKeyType _selectedKeyType;

    private ObservableCollection<SshKeyType> _sshKeyTypes;

    private bool _isKeyFileNameValid = false;
    public bool IsKeyFileNameValid
    {
        get => _isKeyFileNameValid;
        set => this.RaiseAndSetIfChanged(ref _isKeyFileNameValid, value);
    }

    private ValidationHelper _keyNameValidationHelper;

    public ValidationHelper KeyNameValidationHelper
    {
        get => _keyNameValidationHelper;
        set => this.RaiseAndSetIfChanged(ref _keyNameValidationHelper, value);
    }
    
    public AddKeyWindowViewModel()
    {

        _keyNameValidationHelper = this.ValidationRule(
            e => e.KeyName,
            name => !File.Exists(SshConfigFilesExtension.GetBaseSshPath() + Path.DirectorySeparatorChar + name),
            "Filename does already exist!"
        ); 
        AddKey = ReactiveCommand.CreateFromTask<string, AddKeyWindowViewModel?>(async b =>
        {
            _createKey = bool.Parse(b);
            if (!_createKey) return null;
            return !File.Exists(SshConfigFilesExtension.GetBaseSshPath() + Path.DirectorySeparatorChar + KeyName) ? this : null;
        });
        _sshKeyTypes = new ObservableCollection<SshKeyType>(KeyTypeExtension.GetAvailableKeyTypes());
        _selectedKeyType = _sshKeyTypes.First();
    }

    public ReactiveCommand<string, AddKeyWindowViewModel?> AddKey { get; }

    public SshKeyType SelectedKeyType
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

    public ObservableCollection<SshKeyType> SshKeyTypes
    {
        get => _sshKeyTypes;
        set => this.RaiseAndSetIfChanged(ref _sshKeyTypes, value);
    }


    public string KeyName
    {
        get => _keyName;
        set => this.RaiseAndSetIfChanged(ref _keyName, value);
    }

    public string Comment { get; set; } = $"{Environment.UserName}@{Environment.MachineName}";
    public string Password { get; set; } = "";

    public ValidationContext ValidationContext { get; } = new();

    public async ValueTask<SshPublicKey?> RunKeyGen()
    {
        if (!_createKey) return null;
        var fullFilePath = $"{SshConfigFilesExtension.GetBaseSshPath()}{Path.DirectorySeparatorChar}{KeyName}";
        var proc = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                Arguments =
                    $"-t {Enum.GetName(SelectedKeyType.BaseType)!.ToLower()} -C \"{Comment}\" -f \"{fullFilePath}\" -N \"{Password}\" ",
                CreateNoWindow = true,
                FileName = "ssh-keygen",
                RedirectStandardOutput = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = SshConfigFilesExtension.GetBaseSshPath()
            }
        };
        if (!SelectedKeyType.HasDefaultBitSize) proc.StartInfo.Arguments += $"-b {SelectedKeyType.CurrentBitSize} ";
        proc.StartInfo.Arguments += "-q";
        proc.Start();
        await proc.WaitForExitAsync();
        var newKey = new SshPublicKey(fullFilePath + ".pub");
        newKey.GetPrivateKey();
        return proc.ExitCode == 0 ? newKey : null;
    }
}