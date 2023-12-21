using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OpenSSHALib.Enums;
using OpenSSHALib.Extensions;
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
    }

    public ReactiveCommand<string, AddKeyWindowViewModel?> AddKey { get; }

    public IEnumerable<KeyType> IEnumerableEnum { get; set; } = Enum.GetValues<KeyType>();

    private ObservableCollection<int> _possibleByteValues = new(_keyType.GetBitValues());
    public ObservableCollection<int> PossibleByteValues
    {
        get => _possibleByteValues;
        set
        {
            KeyBitSize = value.Max();
            this.RaiseAndSetIfChanged(ref _possibleByteValues, value); // TODO Throws ._.
        }
    }

    public KeyType KeyType
    {
        get => _keyType;
        set
        {
            PossibleByteValues = new ObservableCollection<int>(value.GetBitValues());
            KeyName = $"id_{Enum.GetName(value).ToLower()}";
            this.RaiseAndSetIfChanged(ref _keyType, value);
        }
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
        set => _keyBitSize = value;
        // {
        //     if (_keyBitSize == value) return;
        //     this.RaiseAndSetIfChanged(ref _keyBitSize, value);
        // }
    }

    public async ValueTask<SshKey?> RunKeyGen()
    {
        if (!_createKey) return null;
        var sshUserFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) +
                            $"{Path.DirectorySeparatorChar}.ssh";
        var fullFilePath = $"{sshUserFolder}{Path.DirectorySeparatorChar}{KeyName}";
        var arguments = $"-t {Enum.GetName(KeyType).ToLower()} ";
        if (KeyBitSize != (int)KeyType) arguments += $"-b {KeyBitSize} ";
        arguments += $"-N \"{Password ?? ""}\" -C \"{Comment}\" -f \"{fullFilePath}\" ";
        var proc = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                Arguments = arguments,
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