using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OpenSSHALib.Enums;
using OpenSSHALib.Extensions;
using OpenSSHALib.Lib;
using OpenSSHALib.Models;
using ReactiveUI;

namespace OpenSSHA_GUI.ViewModels;

public class AddKeyWindowViewModel : ViewModelBase
{
    private bool _createKey;

    private string _keyName = "id_rsa";

    private SshKeyType _selectedKeyType;

    private ObservableCollection<SshKeyType> _sshKeyTypes;

    public AddKeyWindowViewModel()
    {
        AddKey = ReactiveCommand.Create<string, AddKeyWindowViewModel?>(b =>
        {
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

    public async ValueTask<SshKey?> RunKeyGen()
    {
        if (!_createKey) return null;
        var fullFilePath = $"{Settings.UserSshFolderPath}{Path.DirectorySeparatorChar}{KeyName}";
        var proc = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                Arguments =
                    $"-t {Enum.GetName(SelectedKeyType.BaseType)!.ToLower()} -b {SelectedKeyType.CurrentBitSize} -N \"{Password}\" -C \"{Comment}\" -f \"{fullFilePath}\" ",
                CreateNoWindow = true,
                FileName = "ssh-keygen",
                RedirectStandardOutput = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = Settings.UserSshFolderPath
            }
        };
        proc.Start();
        await proc.WaitForExitAsync();
        return proc.ExitCode == 0 ? new SshKey(fullFilePath + ".pub") : null;
    }
}