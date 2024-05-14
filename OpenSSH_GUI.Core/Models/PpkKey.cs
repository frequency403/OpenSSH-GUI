#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 14.05.2024 - 00:05:30
// Last edit: 14.05.2024 - 03:05:31

#endregion

using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Interfaces.Keys;
using Renci.SshNet;
using SshNet.Keygen;
using SshNet.Keygen.Extensions;
using SshNet.PuttyKeyFile;

namespace OpenSSH_GUI.Core.Models;

public partial record PpkKey : IPpkKey
{
    private const string EncryptionLineStart = "Encryption:";
    private const string PrivateKeyLineStart = "Private-Lines:";
    private const string PublicKeyLineStart = "Public-Lines:";
    private const string DefinitionLineStart = "PuTTY-User-Key-File-";
    private const string CommentLineStart = "Comment:";
    private const string MacLineStart = "Private-MAC:";
    private readonly IPrivateKeySource _keySource;

    public PpkKey(string absoluteFilePath)
    {
        if (!File.Exists(absoluteFilePath)) return;
        AbsoluteFilePath = absoluteFilePath;
        Filename = Path.GetFileNameWithoutExtension(AbsoluteFilePath);
        _keySource = new PuttyKeyFile(AbsoluteFilePath);
        var lines = File.ReadAllLines(AbsoluteFilePath);

        Format = int.TryParse(
            DefinitionRegex().Replace(lines.FirstOrDefault(e => e.StartsWith(DefinitionLineStart)), "")
                .FirstOrDefault(e => int.TryParse(e.ToString(), out var p)).ToString(),
            out var parsed)
            ? parsed switch
            {
                2 => SshKeyFormat.PuTTYv2,
                3 => SshKeyFormat.PuTTYv3,
                _ => SshKeyFormat.OpenSSH
            }
            : SshKeyFormat.OpenSSH;
        KeyTypeString = DefinitionRegexWithInteger()
            .Replace(lines.FirstOrDefault(e => e.StartsWith(DefinitionLineStart)), "");
        KeyType = new SshKeyType(Enum.Parse<KeyType>(KeyTypeString.Replace("ssh-", "").ToUpper()));
        EncryptionType = Enum.TryParse<EncryptionType>(
            EncryptionRegex().Replace(lines.FirstOrDefault(e => e.StartsWith(EncryptionLineStart)), ""),
            out var parsedEncryptionType)
            ? parsedEncryptionType
            : EncryptionType.NONE;
        Comment = CommentRegex().Replace(lines.FirstOrDefault(e => e.StartsWith(CommentLineStart)) ?? "", "").Trim();
        PrivateKeyString = ExtractLines(lines, PrivateKeyLineStart);
        PublicKeyString = ExtractLines(lines, PublicKeyLineStart);
        PrivateMAC = MacRegex().Replace(lines.FirstOrDefault(e => e.StartsWith(MacLineStart)) ?? "", "")
            .Replace(MacLineStart, "").Trim();
        Fingerprint = PrivateMAC;
    }

    public SshKeyFormat Format { get; }

    public string AbsoluteFilePath { get; private set; }
    public string KeyTypeString { get; }
    public string Filename { get; }
    public ISshKeyType KeyType { get; }
    public string Fingerprint { get; }

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

    public string ExportTextOfKey()
    {
        return ExportPuttyPpkKey();
    }

    public async Task ExportToDiskAsync(SshKeyFormat format)
    {
        var privateFilePath = Path.Combine(Path.GetDirectoryName(AbsoluteFilePath),
            Path.GetFileNameWithoutExtension(AbsoluteFilePath));
        var publicFilePath = Path.ChangeExtension(privateFilePath, ".pub");
        if (File.Exists(privateFilePath)) privateFilePath += DateTime.Now.ToString("yy_MM_dd_HH_mm");
        if (File.Exists(publicFilePath)) publicFilePath = Path.ChangeExtension(privateFilePath, ".pub");

        switch (format)
        {
            case SshKeyFormat.OpenSSH:
                await using (var privateFile = new StreamWriter(privateFilePath))
                {
                    await privateFile.WriteAsync(ExportOpenSshPublicKey());
                }

                await using (var publicFile = new StreamWriter(publicFilePath))
                {
                    await publicFile.WriteAsync(ExportOpenSshPrivateKey());
                }

                break;
            case SshKeyFormat.PuTTYv2:
            case SshKeyFormat.PuTTYv3:
            default:
                break;
        }
    }

    public bool IsPublicKey { get; } = true;

    public string ExportAuthorizedKeyEntry()
    {
        return ExportOpenSshPublicKey();
    }

    public void ExportToDisk(SshKeyFormat format)
    {
        ExportToDiskAsync(format).Wait();
    }

    public Task<string> ExportKeyAsync(bool publicKey = true, SshKeyFormat format = SshKeyFormat.OpenSSH)
    {
        return Task.FromResult(publicKey
            ? format switch
            {
                SshKeyFormat.OpenSSH => _keySource.ToOpenSshPublicFormat(),
                SshKeyFormat.PuTTYv2 or SshKeyFormat.PuTTYv3 => _keySource.ToPuttyPublicFormat(),
                _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
            }
            : format switch
            {
                SshKeyFormat.OpenSSH => _keySource.ToOpenSshFormat(),
                SshKeyFormat.PuTTYv2 or SshKeyFormat.PuTTYv3 => _keySource.ToPuttyFormat(format),
                _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
            });
    }

    public string ExportKey(bool publicKey = true, SshKeyFormat format = SshKeyFormat.OpenSSH)
    {
        return ExportKeyAsync(publicKey, format).Result;
    }

    public EncryptionType EncryptionType { get; }

    public string Comment { get; }

    public string PublicKeyString { get; }

    public string PrivateKeyString { get; }

    public string PrivateMAC { get; }

    public bool IsPuttyKey => Format is not SshKeyFormat.OpenSSH;

    public bool MoveFileToSubFolder([NotNullWhen(false)] out Exception? error)
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
            ExportToDisk(SshKeyFormat.OpenSSH);
            var oldPath = AbsoluteFilePath;
            if (!move) return new SshPublicKey(Path.ChangeExtension(oldPath, ".pub"));
            if (MoveFileToSubFolder(out var ex)) return new SshPublicKey(Path.ChangeExtension(oldPath, ".pub"));
            errorMessage = ex.Message;
            return null;
        }
        catch (Exception e)
        {
            errorMessage = e.Message;
            return null;
        }
    }

    public IPrivateKeySource GetRenciKeyType()
    {
        return _keySource;
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

    [GeneratedRegex(EncryptionLineStart)]
    private static partial Regex EncryptionRegex();

    [GeneratedRegex(DefinitionLineStart)]
    private static partial Regex DefinitionRegex();

    [GeneratedRegex(@$"^{DefinitionLineStart}\d+:")]
    private static partial Regex DefinitionRegexWithInteger();

    [GeneratedRegex(CommentLineStart)]
    private static partial Regex CommentRegex();

    [GeneratedRegex(MacLineStart)]
    private static partial Regex MacRegex();
}