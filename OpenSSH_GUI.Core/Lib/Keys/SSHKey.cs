#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:25

#endregion

using System.Diagnostics;
using System.Text.RegularExpressions;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Interfaces.Keys;
using OpenSSH_GUI.Core.Lib.Abstract;
using SshNet.Keygen;

namespace OpenSSH_GUI.Core.Lib.Keys;

public abstract partial class SshKey : KeyBase, ISshKey
{
    protected SshKey(string absoluteFilePath) : base(absoluteFilePath)
    {
        if (!File.Exists(AbsoluteFilePath)) throw new FileNotFoundException($"No such file: {AbsoluteFilePath}");
        Filename = Path.GetFileName(AbsoluteFilePath);
        var outputOfProcess = ReadSshFile(ref absoluteFilePath).Split(' ').ToList();
        var intToParse = outputOfProcess.First();
        outputOfProcess.RemoveRange(0, 2);
        var keyTypeText = BracesRegex().Replace(outputOfProcess.Last().Trim(), "$1");
        outputOfProcess.RemoveAt(outputOfProcess.Count - 1);

        Comment = string.Join(" ", outputOfProcess);

        if (Enum.TryParse<KeyType>(keyTypeText, true, out var parsedEnum))
        {
            if (int.TryParse(intToParse, out _)) KeyType = new SshKeyType(parsedEnum);
        }
        else
        {
            throw new ArgumentException($"{keyTypeText} is not a valid enum member of {typeof(KeyType)}");
        }

        Format = SshKeyFormat.OpenSSH;
    }

    public bool IsPublicKey => AbsoluteFilePath.EndsWith(".pub");
    public string KeyTypeString => IsPublicKey ? "public" : "private";
    public string Filename { get; }
    public string Comment { get; }
    public ISshKeyType KeyType { get; } = new SshKeyType(Enums.KeyType.RSA);
    public bool IsPuttyKey => Format is not SshKeyFormat.OpenSSH;

    public override string ExportTextOfKey()
    {
        return this is ISshPublicKey ? ExportOpenSshPublicKey() : ExportOpenSshPrivateKey();
    }

    [GeneratedRegex(@"\(([^)]*)\)")]
    private static partial Regex BracesRegex();

    private string ReadSshFile(ref string filePath)
    {
        using var readerProcess = new Process();
        readerProcess.StartInfo = new ProcessStartInfo
        {
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            Arguments = $"-l -f {filePath}",
            FileName = "ssh-keygen"
        };
        readerProcess.Start();
        return readerProcess.StandardOutput.ReadToEnd();
    }
}