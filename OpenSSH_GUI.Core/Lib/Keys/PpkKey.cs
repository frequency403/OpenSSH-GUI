#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:26

#endregion

using System.Text.RegularExpressions;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Interfaces.Keys;
using OpenSSH_GUI.Core.Lib.Abstract;
using SshNet.Keygen;

namespace OpenSSH_GUI.Core.Lib.Keys;

public partial class PpkKey : KeyBase, IPpkKey
{
    private const string EncryptionLineStart = "Encryption:";
    private const string PrivateKeyLineStart = "Private-Lines:";
    private const string PublicKeyLineStart = "Public-Lines:";
    private const string DefinitionLineStart = "PuTTY-User-Key-File-";
    private const string CommentLineStart = "Comment:";
    private const string MacLineStart = "Private-MAC:";

    public PpkKey(string absoluteFilePath) : base(absoluteFilePath)
    {
        if (!File.Exists(absoluteFilePath)) return;
        Filename = Path.GetFileNameWithoutExtension(AbsoluteFilePath);
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
    }

    public string KeyTypeString { get; }
    public string Filename { get; }
    public ISshKeyType KeyType { get; }
    public bool IsPublicKey { get; } = true;
    public EncryptionType EncryptionType { get; }
    public string Comment { get; }
    public string PublicKeyString { get; }
    public string PrivateKeyString { get; }
    public string PrivateMAC { get; }
    public bool IsPuttyKey => Format is not SshKeyFormat.OpenSSH;

    public override string ExportTextOfKey()
    {
        return ExportPuttyPpkKey();
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