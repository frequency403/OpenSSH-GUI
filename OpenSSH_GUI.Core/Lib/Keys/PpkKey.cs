#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:26

#endregion

using System.Text.RegularExpressions;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Interfaces.Keys;
using OpenSSH_GUI.Core.Lib.Abstract;
using OpenSSH_GUI.Core.Lib.Static;
using SshNet.Keygen;

namespace OpenSSH_GUI.Core.Lib.Keys;

public partial class PpkKey : KeyBase, IPpkKey
{
    /// <summary>
    ///     The start of the line that indicates the encryption type in a PpkKey file.
    /// </summary>
    private const string EncryptionLineStart = "Encryption:";

    /// <summary>
    ///     The starting line indicator for the private key in a PPK key file.
    /// </summary>
    private const string PrivateKeyLineStart = "Private-Lines:";

    /// <summary>
    ///     Represents the starting line of the public key in a PPK key file.
    /// </summary>
    private const string PublicKeyLineStart = "Public-Lines:";

    /// <summary>
    ///     Represents the starting line of the definition in a PpkKey file.
    /// </summary>
    private const string DefinitionLineStart = "PuTTY-User-Key-File-";

    /// <summary>
    ///     Represents the starting keyword of the comment line in a PPK key file.
    /// </summary>
    private const string CommentLineStart = "Comment:";

    /// <summary>
    ///     The start of the line that represents the private MAC in a PPK key file.
    /// </summary>
    private const string MacLineStart = "Private-MAC:";

    /// <summary>
    ///     Extracts key information from a PPK key file.
    /// </summary>
    /// <param name="absoluteFilePath">The absolute file path of the PPK key file.</param>
    /// <param name="password">The password to decrypt the private key (optional).</param>
    public PpkKey(string absoluteFilePath, string? password = null) : base(absoluteFilePath, password)
    {
        var lines = FileOperations.ReadAllLines(AbsoluteFilePath);

        Format = int.TryParse(
            DefinitionRegex().Replace(lines.FirstOrDefault(e => e.StartsWith(DefinitionLineStart))!, "")
                .FirstOrDefault(e => int.TryParse(e.ToString(), out _)).ToString(),
            out var parsed)
            ? parsed switch
            {
                2 => SshKeyFormat.PuTTYv2,
                3 => SshKeyFormat.PuTTYv3,
                _ => SshKeyFormat.OpenSSH
            }
            : SshKeyFormat.OpenSSH;
        KeyTypeString = DefinitionRegexWithInteger()
            .Replace(lines.FirstOrDefault(e => e.StartsWith(DefinitionLineStart))!, "");
        EncryptionType = Enum.TryParse<EncryptionType>(
            EncryptionRegex().Replace(lines.FirstOrDefault(e => e.StartsWith(EncryptionLineStart))!, ""),
            out var parsedEncryptionType)
            ? parsedEncryptionType
            : EncryptionType.NONE;
        Comment = CommentRegex().Replace(lines.FirstOrDefault(e => e.StartsWith(CommentLineStart)) ?? "", "").Trim();
        PrivateKeyString = ExtractLines(lines, PrivateKeyLineStart);
        PublicKeyString = ExtractLines(lines, PublicKeyLineStart);
        PrivateMAC = MacRegex().Replace(lines.FirstOrDefault(e => e.StartsWith(MacLineStart)) ?? "", "")
            .Replace(MacLineStart, "").Trim();
    }

    /// <summary>
    ///     Represents a key with a string representation of its type.
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public string KeyTypeString { get; }

    /// <summary>
    ///     Represents the encryption type used in a PPK key.
    /// </summary>
    public EncryptionType EncryptionType { get; }

    /// <summary>
    ///     Gets the comment associated with the SSH key.
    /// </summary>
    public string Comment { get; }

    /// <summary>
    ///     Gets the public key string of the PPK key.
    /// </summary>
    /// <value>
    ///     The public key string.
    /// </value>
    public string PublicKeyString { get; }

    /// <summary>
    ///     Gets the private key string.
    /// </summary>
    public string PrivateKeyString { get; }

    /// <summary>
    ///     The MAC (Message Authentication Code) used for the private key in a PpkKey file.
    /// </summary>
    public string PrivateMAC { get; }

    /// <summary>
    ///     Gets a value indicating whether the key is a PuTTY key.
    /// </summary>
    /// <value><c>true</c> if the key is a PuTTY key; otherwise, <c>false</c>.</value>
    public bool IsPuttyKey => Format is not SshKeyFormat.OpenSSH;

    /// <summary>
    ///     Exports the text representation of the key.
    /// </summary>
    /// <returns>The text representation of the key.</returns>
    public override string ExportTextOfKey()
    {
        return ExportPuttyPpkKey()!;
    }

    /// <summary>
    ///     Extracts a subset of lines from an array of strings based on a marker.
    /// </summary>
    /// <param name="lines">The array of strings to extract lines from.</param>
    /// <param name="marker">The marker used to identify the lines to extract.</param>
    /// <returns>A string containing the extracted lines.</returns>
    private string ExtractLines(string[] lines, string marker)
    {
        var startPosition = 0;
        var linesToExtract = 0;
        foreach (var line in lines.Select((content, index) => (content, index)))
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

    /// <summary>
    ///     Extracts the key type string from the definition line of a PPK key file.
    /// </summary>
    /// <returns>The key type string.</returns>
    [GeneratedRegex(@$"^{DefinitionLineStart}\d+:")]
    private static partial Regex DefinitionRegexWithInteger();

    /// <summary>
    ///     Extracts the comment from a line starting with "Comment:".
    /// </summary>
    /// <returns>The comment extracted from the line.</returns>
    [GeneratedRegex(CommentLineStart)]
    private static partial Regex CommentRegex();

    /// <summary>
    ///     Represents a regular expression used to extract the MAC string from a PPK key file.
    /// </summary>
    /// <returns>A regular expression object.</returns>
    [GeneratedRegex(MacLineStart)]
    private static partial Regex MacRegex();
}