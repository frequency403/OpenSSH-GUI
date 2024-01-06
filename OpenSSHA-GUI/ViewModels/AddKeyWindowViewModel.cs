using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using OpenSSHALib.Enums;
using OpenSSHALib.Extensions;
using OpenSSHALib.Lib;
using OpenSSHALib.Models;
using ReactiveUI;
using ReactiveUI.Validation.Abstractions;
using ReactiveUI.Validation.Contexts;
using ReactiveUI.Validation.Extensions;

namespace OpenSSHA_GUI.ViewModels;

public class AddKeyWindowViewModel : ViewModelBase, IValidatableViewModel
{
    private bool _createKey;

    private string _keyName = "id_rsa";

    private SshKeyType _selectedKeyType;

    private ObservableCollection<SshKeyType> _sshKeyTypes;

    public AddKeyWindowViewModel()
    {

        this.ValidationRule(
            e => e.KeyName,
            name => File.Exists(SettingsFileHandler.Settings.UserSshFolderPath + Path.DirectorySeparatorChar + name),
            "Filename does already exist!"
        ); // TODO: Validation does not yet work correctly, need further fixing.
        
        AddKey = ReactiveCommand.CreateFromTask<string, AddKeyWindowViewModel?>(async b =>
        {
            if (File.Exists(SettingsFileHandler.Settings.UserSshFolderPath + Path.DirectorySeparatorChar + KeyName))
            {
                var box = MessageBoxManager.GetMessageBoxStandard("Keyfile does already exists",
                    "The filename you requested exists already. Aborting.", ButtonEnum.Ok, Icon.Error);
                await box.ShowAsync();
                return null;
            } // TODO: Remove, when Validation works.
            _createKey = bool.Parse(b);
            return this;
        });
        _sshKeyTypes = new ObservableCollection<SshKeyType>(KeyTypeExtension.GetAvailableKeyTypes());
        _selectedKeyType = _sshKeyTypes.First(e => e.BaseType == KeyType.RSA);
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

    public async ValueTask<SshPublicKey?> RunKeyGen()
    {
        if (!_createKey) return null;
        var fullFilePath = $"{SettingsFileHandler.Settings.UserSshFolderPath}{Path.DirectorySeparatorChar}{KeyName}";
        var proc = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                Arguments =
                    $"-t {Enum.GetName(SelectedKeyType.BaseType)!.ToLower()} -b {SelectedKeyType.CurrentBitSize} -N \"{Password}\" -C \"{Comment}\" -f \"{fullFilePath}\"",
                CreateNoWindow = true,
                FileName = "ssh-keygen",
                RedirectStandardOutput = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = SettingsFileHandler.Settings.UserSshFolderPath
            }
        };
        proc.Start();
        await proc.WaitForExitAsync();
        var newKey = new SshPublicKey(fullFilePath + ".pub");
        newKey.GetPrivateKey();
        return proc.ExitCode == 0 ? newKey : null;
    }

    public ValidationContext ValidationContext { get; } = new ();
}