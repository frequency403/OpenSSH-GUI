#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 08.05.2024 - 22:05:30
// Last edit: 08.05.2024 - 22:05:58

#endregion

using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using OpenSSHALib.Enums;
using OpenSSHALib.Interfaces;
using Renci.SshNet;
using Renci.SshNet.Security;
using SshNet.Keygen;
using SshNet.Keygen.Extensions;

namespace OpenSSHALib.Models;

public abstract partial class SshKey : ISshKey
{
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
    }

    public SshKeyFormat Format { get; }
    public string AbsoluteFilePath { get; protected set; }
    public bool IsPublicKey => AbsoluteFilePath.EndsWith(".pub");
    public string KeyTypeString => IsPublicKey ? "public" : "private";
    public string Filename { get; protected set; }
    public string Comment { get; protected set; }
    public ISshKeyType KeyType { get; } = new SshKeyType(Enums.KeyType.RSA);
    public string Fingerprint { get; protected set; }
    public bool IsPuttyKey  => Format is not SshKeyFormat.OpenSSH;

    public string ExportAuthorizedKeyEntry()
    {
        return new PrivateKeyFile(this is ISshPublicKey pub ? pub.AbsoluteFilePath.Replace(".pub", "") : AbsoluteFilePath).ToOpenSshPublicFormat();
    }
    
    public async Task<string> ExportKeyAsync(SshKeyFormat format = SshKeyFormat.OpenSSH)
    {
        try
        {
            await using var fileStream = File.OpenRead(AbsoluteFilePath);
            using var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);
            return Encoding.Default.GetString(memoryStream.ToArray());
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            return null;
        }
    }

    public void DeleteKey()
    {
        if (this is ISshPublicKey pub) pub.PrivateKey.DeleteKey();
        File.Delete(AbsoluteFilePath);
    }

    public string ExportKey(SshKeyFormat format = SshKeyFormat.OpenSSH)
    {
        return ExportKeyAsync().Result;
    }

    public abstract IPrivateKeySource GetRenciKeyType();

    public ISshKey Convert(SshKeyFormat format)
    {
        if (format == Format) return this;
        var priv = this is ISshPublicKey pubk
            ? new PrivateKeyFile(pubk.PrivateKey.AbsoluteFilePath)
            : new PrivateKeyFile(AbsoluteFilePath);
        priv.ToPuttyFormat();
        File.WriteAllText(Path.Combine(Path.GetFullPath(AbsoluteFilePath), Path.ChangeExtension(Path.GetFileNameWithoutExtension(AbsoluteFilePath),".ppk")), priv.ToPuttyFormat());
        return this;
    }
    
    public ISshKey? Convert(SshKeyFormat format, ILogger logger)
    {
        if (format.Equals(Format)) return this;
        
        try
        {
            var priv = this is ISshPublicKey pubk
                ? new PrivateKeyFile(pubk.PrivateKey.AbsoluteFilePath)
                : new PrivateKeyFile(AbsoluteFilePath);
           var pathToWrite = Path.Combine(Path.GetDirectoryName(AbsoluteFilePath),
                Path.ChangeExtension(Path.GetFileNameWithoutExtension(AbsoluteFilePath), ".ppk"));
            File.WriteAllText(pathToWrite, priv.ToPuttyFormat());
            return new PpkKey(pathToWrite);
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