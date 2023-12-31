using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OpenSSHALib.Enums;
using OpenSSHALib.Extensions;
using OpenSSHALib.Lib;
using OpenSSHALib.Model;
using ReactiveUI;

namespace OpenSSHA_GUI.ViewModels;

public class AddKeyWindowViewModel : ViewModelBase
{
    private static KeyType _keyType = KeyType.RSA;

    private bool _createKey;

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

    public IEnumerable<KeyType> IEnumerableEnum { get; set; } = Enum.GetValues<KeyType>();

    private ObservableCollection<int> _possibleByteValues = new(_keyType.GetBitValues());
    public ObservableCollection<int> PossibleByteValues
    {
        get => _possibleByteValues;
        set
        {
            //KeyBitSize = 0;
             KeyBitSize = value.Max();
            this.RaiseAndSetIfChanged(ref _possibleByteValues, value); // TODO Throws ._.
        }
    }

    public KeyType KeyType
    {
        get => _keyType;
        set
        {
            // PossibleByteValues = new ObservableCollection<int>(value.GetBitValues());
            KeyBitSize = value.GetBitValues().Max();
            KeyName = $"id_{Enum.GetName(value).ToLower()}";
            this.RaiseAndSetIfChanged(ref _keyType, value);
        }
    }

    private SshKeyType _selectedKeyType;

    public SshKeyType SelectedKeyType
    {
        get => _selectedKeyType;
        set 
        {
            try
            {
                KeyName = $"id_{Enum.GetName(value.BaseType).ToLower()}";
                this.RaiseAndSetIfChanged(ref _selectedKeyType, value);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }   
        }
    }

    private ObservableCollection<SshKeyType> _sshKeyTypes;

    public ObservableCollection<SshKeyType> SshKeyTypes
    {
        get => _sshKeyTypes;
        set => this.RaiseAndSetIfChanged(ref _sshKeyTypes, value);
    }

    private string _keyName = "id_rsa";
    public string KeyName
    {
        get => _keyName;
        set => this.RaiseAndSetIfChanged(ref _keyName, value);
    }
    public string Comment { get; set; } = $"{Environment.UserName}@{Environment.MachineName}";
    public string Password { get; set; } = "";

    private int _keyBitSize = 2048;
    public int KeyBitSize
    {
        get => _keyBitSize;
        set //=> _keyBitSize = value;
        {
            if (_keyBitSize == value) return;
            this.RaiseAndSetIfChanged(ref _keyBitSize, value);
        }
    }
    
    // public int KeyBitSize { get; set; }

    public async ValueTask<SshKey?> RunKeyGen()
    {
        if (!_createKey) return null;
        var sshUserFolder = Settings.UserSshFolderPath;
        var fullFilePath = $"{sshUserFolder}{Path.DirectorySeparatorChar}{KeyName}";
        KeyBitSize = KeyBitSize == 0 ? (int)KeyType : KeyBitSize;
        var proc = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                Arguments = $"-t {Enum.GetName(KeyType).ToLower()} -b {SelectedKeyType.CurrentBitSize} -N \"{Password}\" -C \"{Comment}\" -f \"{fullFilePath}\" ",
                CreateNoWindow = true,
                FileName = "ssh-keygen",
                RedirectStandardOutput = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                WorkingDirectory = sshUserFolder
            }
        };
        proc.Start();
        await proc.WaitForExitAsync();
        return proc.ExitCode == 0 ? new SshKey(fullFilePath + ".pub") : null;
    }
}