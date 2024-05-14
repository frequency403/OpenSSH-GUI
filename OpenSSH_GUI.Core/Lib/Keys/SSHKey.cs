#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 14.05.2024 - 00:05:30
// Last edit: 14.05.2024 - 03:05:22

#endregion

using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Interfaces.AuthorizedKeys;
using OpenSSH_GUI.Core.Interfaces.Keys;
using OpenSSH_GUI.Core.Lib.AuthorizedKeys;
using Renci.SshNet;
using SshNet.Keygen;
using SshNet.Keygen.Extensions;

namespace OpenSSH_GUI.Core.Lib.Keys;

public abstract partial class SshKey : ISshKey
{
    private readonly IPrivateKeySource _keySource;

    protected SshKey(string absoluteFilePath)
    {
        AbsoluteFilePath = absoluteFilePath;
        if (!File.Exists(AbsoluteFilePath)) throw new FileNotFoundException($"No such file: {AbsoluteFilePath}");
        Filename = Path.GetFileName(AbsoluteFilePath);
        var outputOfProcess = ReadSshFile(ref absoluteFilePath).Split(' ').ToList();
        var intToParse = outputOfProcess.First();
        outputOfProcess.RemoveAt(0);

        Fingerprint = outputOfProcess.First();
        outputOfProcess.RemoveAt(0);

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
        _keySource = new PrivateKeyFile(IsPublicKey ? AbsoluteFilePath.Replace(".pub", "") : AbsoluteFilePath);
    }

    public SshKeyFormat Format { get; }
    public string AbsoluteFilePath { get; }
    public bool IsPublicKey => AbsoluteFilePath.EndsWith(".pub");
    public string KeyTypeString => IsPublicKey ? "public" : "private";
    public string Filename { get; }
    public string Comment { get; }
    public ISshKeyType KeyType { get; } = new SshKeyType(Enums.KeyType.RSA);
    public string Fingerprint { get; }
    public bool IsPuttyKey => Format is not SshKeyFormat.OpenSSH;

    public string ExportOpenSshPublicKey()
    {
        return _keySource.ToOpenSshPublicFormat();
    }

    public string ExportOpenSshPrivateKey()
    {
        return _keySource.ToOpenSshFormat();
    }

    public string ExportPuttyPublicKey()
    {
        return _keySource.ToPuttyPublicFormat();
    }

    public string ExportPuttyPpkKey()
    {
        return _keySource.ToPuttyFormat();
    }

    public string? ExportTextOfKey()
    {
        if (this is ISshPublicKey) return ExportOpenSshPublicKey();
        if (this is ISshPrivateKey) return ExportOpenSshPrivateKey();
        return null;
    }


    public string ExportAuthorizedKeyEntry()
    {
        return ExportOpenSshPublicKey();
    }

    public IAuthorizedKey ExportAuthorizedKey()
    {
        return new AuthorizedKey(ExportAuthorizedKeyEntry());
    }

    public async Task ExportToDiskAsync(SshKeyFormat format = SshKeyFormat.OpenSSH)
    {
        if (format.Equals(Format)) return;
        var privateFilePath = Path.Combine(Path.GetDirectoryName(AbsoluteFilePath),
            Path.GetFileNameWithoutExtension(AbsoluteFilePath));
        if (format is not SshKeyFormat.OpenSSH) privateFilePath = Path.ChangeExtension(privateFilePath, ".ppk");
        switch (format)
        {
            case SshKeyFormat.PuTTYv2:
            case SshKeyFormat.PuTTYv3:
                await using (var privateFile = new StreamWriter(privateFilePath))
                {
                    await privateFile.WriteAsync(_keySource.ToPuttyFormat());
                }

                break;
            case SshKeyFormat.OpenSSH:
            default:
                break;
        }
    }

    public void DeleteKey()
    {
        if (this is ISshPublicKey pub) pub.PrivateKey.DeleteKey();
        File.Delete(AbsoluteFilePath);
    }

    public void ExportToDisk(SshKeyFormat format = SshKeyFormat.OpenSSH)
    {
        ExportToDiskAsync(format).Wait();
    }

    public IPrivateKeySource GetRenciKeyType()
    {
        return _keySource;
    }

    public ISshKey Convert(SshKeyFormat format)
    {
        if (format == Format) return this;
        ExportToDisk(SshKeyFormat.PuTTYv3);
        return new PpkKey(Path.ChangeExtension(AbsoluteFilePath, ".ppk"));
    }

    public ISshKey? Convert(SshKeyFormat format, ILogger logger)
    {
        if (format.Equals(Format)) return this;

        try
        {
            ExportToDisk(SshKeyFormat.PuTTYv3);
            return new PpkKey(Path.ChangeExtension(AbsoluteFilePath, ".ppk"));
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error converting the key");
        }

        return null;
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