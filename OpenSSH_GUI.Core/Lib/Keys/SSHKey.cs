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

/// <summary>
/// Represents an SSH key.
/// </summary>
public abstract partial class SshKey : KeyBase, ISshKey
{
    /// <summary>
    /// Represents a SSH key.
    /// </summary>
    protected SshKey(string absoluteFilePath, string? password = null) : base(absoluteFilePath, password)
    {
        if (!File.Exists(AbsoluteFilePath)) throw new FileNotFoundException($"No such file: {AbsoluteFilePath}");
        var outputOfProcess = ReadSshFile(ref absoluteFilePath).Split(' ').ToList();
        var intToParse = outputOfProcess.First();
        outputOfProcess.RemoveRange(0, 2);
        var keyTypeText = BracesRegex().Replace(outputOfProcess.Last().Trim(), "$1");
        outputOfProcess.Remove(outputOfProcess.Last());

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

    /// <summary>
    /// Gets a value indicating whether the key is a public key.
    /// </summary>
    /// <value><c>true</c> if the key is a public key; otherwise, <c>false</c>.</value>
    public bool IsPublicKey => AbsoluteFilePath.EndsWith(".pub");

    /// <summary>
    /// Gets the type of the key as a string.
    /// </summary>
    /// <value>The type of the key as a string.</value>
    public string KeyTypeString => IsPublicKey ? "public" : "private";

    /// <summary>
    /// Gets the comment associated with the SSH key.
    /// </summary>
    /// <value>The comment.</value>
    public string Comment { get; }

    /// <summary>
    /// Represents the type of an SSH key.
    /// </summary>
    public ISshKeyType KeyType { get; } = new SshKeyType(Enums.KeyType.RSA);

    /// <summary>
    /// Gets a value indicating whether the key is a Putty key.
    /// </summary>
    public bool IsPuttyKey => Format is not SshKeyFormat.OpenSSH;

    /// <summary>
    /// Exports the text representation of the SSH key.
    /// </summary>
    /// <returns>The text representation of the SSH key.</returns>
    public override string ExportTextOfKey()
    {
        return this is ISshPublicKey ? ExportOpenSshPublicKey() : ExportOpenSshPrivateKey();
    }

    /// <summary>
    /// Extracts the text enclosed in parentheses from a given string.
    /// </summary>
    /// <returns>A regular expression pattern that matches text enclosed in parentheses.</returns>
    [GeneratedRegex(@"\(([^)]*)\)")]
    private static partial Regex BracesRegex();

    /// <summary>
    /// Reads the contents of an SSH file.
    /// </summary>
    /// <param name="filePath">The absolute file path of the SSH file to read.</param>
    /// <returns>The contents of the SSH file.</returns>
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