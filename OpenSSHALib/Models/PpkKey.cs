﻿#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 08.05.2024 - 22:05:30
// Last edit: 08.05.2024 - 22:05:54

#endregion

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using OpenSSHALib.Enums;
using OpenSSHALib.Interfaces;
using Renci.SshNet;
using SshNet.Keygen;
using SshNet.Keygen.Extensions;
using SshNet.PuttyKeyFile;

namespace OpenSSHALib.Models;

public record PpkKey : IPpkKey
{
    private const string EncryptionLineStart = "Encryption:";
    private const string PrivateKeyLineStart = "Private-Lines:";
    private const string PublicKeyLineStart = "Public-Lines:";
    private const string DefinitionLineStart = "PuTTY-User-Key-File-";
    private const string CommentLineStart = "Comment:";
    private const string MacLineStart = "Private-MAC:";
    private readonly PuttyKeyFile _keyFile;

    public PpkKey(string absoluteFilePath)
    {
        if (!File.Exists(absoluteFilePath)) return;
        AbsoluteFilePath = absoluteFilePath;
        Filename = Path.GetFileNameWithoutExtension(AbsoluteFilePath);
        _keyFile = new PuttyKeyFile(AbsoluteFilePath);
        var lines = File.ReadAllLines(AbsoluteFilePath);

        Format = int.TryParse(
            lines.FirstOrDefault(e => e.StartsWith(DefinitionLineStart)).Replace(DefinitionLineStart, "")[0].ToString(),
            out var parsed)
            ? parsed switch
            {
                2 => SshKeyFormat.PuTTYv2,
                3 => SshKeyFormat.PuTTYv3,
                _ => SshKeyFormat.OpenSSH
            }
            : SshKeyFormat.OpenSSH;
        KeyTypeString = lines.FirstOrDefault(e => e.StartsWith(DefinitionLineStart)).Split('-')[0].Trim();
        KeyType = new SshKeyType(
            Enum.TryParse<KeyType>(lines.FirstOrDefault(e => e.StartsWith(DefinitionLineStart)).Split('-')[0].Trim(),
                out var parsedKeyType)
                ? parsedKeyType
                : Enums.KeyType.RSA);
        EncryptionType = Enum.TryParse<EncryptionType>(
            lines.FirstOrDefault(e => e.StartsWith(EncryptionLineStart)).Replace(EncryptionLineStart, "").Trim(),
            out var parsedEncryptionType)
            ? parsedEncryptionType
            : EncryptionType.NONE;
        Comment = (lines.FirstOrDefault(e => e.StartsWith(CommentLineStart)) ?? "").Replace(CommentLineStart, "")
            .Trim();
        PrivateKeyString = ExtractLines(lines, PrivateKeyLineStart);
        PublicKeyString = ExtractLines(lines, PublicKeyLineStart);
        PrivateMAC = (lines.FirstOrDefault(e => e.StartsWith(MacLineStart)) ?? "").Replace(MacLineStart, "").Trim();
        Fingerprint = PrivateMAC;
    }

    public SshKeyFormat Format { get; }

    public string AbsoluteFilePath { get; private set; }
    public string KeyTypeString { get; }
    public string Filename { get; }
    public ISshKeyType KeyType { get; }
    public string Fingerprint { get; }
    public bool IsPublicKey { get; } = true;

    public string ExportAuthorizedKeyEntry()
    {
        return _keyFile.ToOpenSshPublicFormat();
    }
    
    public Task<string> ExportKeyAsync(bool publicKey = true, SshKeyFormat format = SshKeyFormat.OpenSSH)
    {
        return Task.FromResult(publicKey
            ? format switch
            {
                SshKeyFormat.OpenSSH => _keyFile.ToOpenSshPublicFormat(),
                SshKeyFormat.PuTTYv2 or SshKeyFormat.PuTTYv3 => _keyFile.ToPuttyPublicFormat(),
                _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
            }
            : format switch
            {
                SshKeyFormat.OpenSSH => _keyFile.ToOpenSshFormat(),
                SshKeyFormat.PuTTYv2 or SshKeyFormat.PuTTYv3 => _keyFile.ToPuttyFormat(format),
                _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
            });
    }

    public string ExportKey(bool publicKey = true, SshKeyFormat format = SshKeyFormat.OpenSSH)
    {
        return ExportKeyAsync(publicKey, format).Result;
    }

    public Task<string> ExportKeyAsync(SshKeyFormat format = SshKeyFormat.OpenSSH)
    {
        return ExportKeyAsync(true, format);
    }

    public string ExportKey(SshKeyFormat format = SshKeyFormat.OpenSSH)
    {
        return ExportKeyAsync(format).Result;
    }

    public EncryptionType EncryptionType { get; }

    public string Comment { get; }

    public string PublicKeyString { get; }

    public string PrivateKeyString { get; }

    public string PrivateMAC { get; }
    
    public bool IsPuttyKey => Format is not SshKeyFormat.OpenSSH;

    public bool MoveFileToSubFolder([NotNullWhen(false)]out Exception? error)
    {
        error = null;
        try
        {
            var directory = Directory.GetParent(AbsoluteFilePath)!.CreateSubdirectory("PPK");
            var newFileDestination = Path.Combine(directory.FullName, Path.GetFileName(AbsoluteFilePath));
            File.Move(AbsoluteFilePath, newFileDestination);
            AbsoluteFilePath = newFileDestination;
            return true;
        }
        catch (Exception e)
        {
            error = e;
            return false;
        }
    }
    
    public ISshPublicKey? ConvertToOpenSshKey(out string errorMessage, bool temp = false, bool move = true)
    {
        errorMessage = "";
        try
        {
            var privateFilePath = Path.Combine(Path.GetDirectoryName(AbsoluteFilePath), Path.GetFileNameWithoutExtension(AbsoluteFilePath));
            var publicFilePath = Path.ChangeExtension(privateFilePath, ".pub");
            if (File.Exists(privateFilePath)) privateFilePath += DateTime.Now.ToString("yy_MM_dd_HH_mm");
            if (File.Exists(publicFilePath)) publicFilePath = Path.ChangeExtension(privateFilePath, ".pub");
            if (move)
            {
                if (!MoveFileToSubFolder(out var ex))
                {
                    errorMessage = ex.Message;
                    return null;
                }
            }
            File.WriteAllText(privateFilePath, _keyFile.ToOpenSshFormat());
            File.WriteAllText(publicFilePath, _keyFile.ToOpenSshPublicFormat());
            return new SshPublicKey(publicFilePath);
        }
        catch (Exception e)
        {
            errorMessage = e.Message;
            return null;
        }
    }

    public IPrivateKeySource GetRenciKeyType()
    {
        return _keyFile;
    }

    public void DeleteKey()
    {
        File.Delete(AbsoluteFilePath);
    }
    
    public ISshKey? Convert(SshKeyFormat format)
    {
        if (format.Equals(Format)) return this;
        return ConvertToOpenSshKey(out _, move: false);
    }

    public ISshKey? Convert(SshKeyFormat format, ILogger logger)
    {
        if (format.Equals(Format)) return this;
        var convertResult = ConvertToOpenSshKey(out var errorMessage, move: false);
        if (string.IsNullOrWhiteSpace(errorMessage)) return convertResult;
        logger.LogError("Error converting the key -> {0}", errorMessage);
        return null;
    }

    private string ExtractLines(string[] lines, string marker)
    {
        var startPosition = 0;
        var linesToExtract = 0;
        foreach (var line in lines.Select((content, index) => (content, index)))
            if (startPosition == 0)
                if (line.content.Contains(marker))
                {
                    linesToExtract = int.Parse(line.content.Replace(marker, "").Trim());
                    startPosition = line.index + 1;
                    break;
                }

        return string.Join("", lines, startPosition, linesToExtract);
    }

    // public override string ToString()
    // {
    //     return $"";
    // }
}