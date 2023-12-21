using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OpenSSHALib.Enums;
using OpenSSHALib.Model;
using ReactiveUI;

namespace OpenSSHA_GUI.ViewModels;

public class AddKeyWindowViewModel : ViewModelBase
{
    public AddKeyWindowViewModel()
    {
        AddKey = ReactiveCommand.Create<string, AddKeyWindowViewModel?>(b =>
        {
            CreateKey = bool.Parse(b);
            return this;
        });
    }

    public ReactiveCommand<string, AddKeyWindowViewModel?> AddKey { get; }

    public IEnumerable<KeyType> IEnumerableEnum { get; set; }= Enum.GetValues<KeyType>();
    
    private static KeyType _keyType = KeyType.Rsa;

    public KeyType KeyType
    {
        get => _keyType;
        set
        {
            BitSize = (int)value;
            this.RaiseAndSetIfChanged(ref _keyType, value);
        }
    }

    public string KeyName { get; set; } = "id_rsa";
    public string Comment { get; set; } = $"{Environment.UserName}@{Environment.MachineName}";
    public string? Password { get; set; } = null;

    private int _bitSize = (int)_keyType;
    public int BitSize
    {
        get => _bitSize;
        set => this.RaiseAndSetIfChanged(ref _bitSize, value);
    }

    public bool CreateKey;
    
    public async ValueTask<SshKey?> RunKeyGen()
    {
        if(!CreateKey) return null;
        var sshUserFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) +
                            $"{Path.DirectorySeparatorChar}.ssh";
        var fullFilePath = $"{sshUserFolder}{Path.DirectorySeparatorChar}{KeyName}";
        var arguments = $"-t {Enum.GetName(KeyType).ToLower()} ";
        if (BitSize != (int)KeyType) arguments += $"-b {BitSize} ";
        arguments += $"-N \"{Password??""}\" -C \"{Comment}\" -f \"{fullFilePath}\" ";
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
        return proc.ExitCode == 0 ? new SshKey(fullFilePath+ ".pub") : null;
    }
}