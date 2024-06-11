#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:25

#endregion

using System.Diagnostics;
using OpenSSH_GUI.Core.Interfaces.Keys;
using OpenSSH_GUI.Core.Lib.Abstract;
using OpenSSH_GUI.Core.Lib.Static;
using SshNet.Keygen;

namespace OpenSSH_GUI.Core.Lib.Keys;

/// <summary>
///     Represents an SSH key.
/// </summary>
public abstract class SshKey : KeyBase, ISshKey
{
    /// <summary>
    ///     Represents a SSH key.
    /// </summary>
    protected SshKey(string absoluteFilePath, string? password = null) : base(absoluteFilePath, password)
    {
        if (!FileOperations.Exists(AbsoluteFilePath))
            throw new FileNotFoundException($"No such file: {AbsoluteFilePath}");
        var outputOfProcess = ReadSshFile(ref absoluteFilePath).Split(' ').ToList();
        outputOfProcess.RemoveRange(0, 2);
        outputOfProcess.Remove(outputOfProcess.Last());
        Comment = string.Join(" ", outputOfProcess);
        Format = SshKeyFormat.OpenSSH;
    }

    /// <summary>
    ///     Gets the comment associated with the SSH key.
    /// </summary>
    /// <value>The comment.</value>
    public string Comment { get; }


    /// <summary>
    ///     Gets a value indicating whether the key is a Putty key.
    /// </summary>
    public bool IsPuttyKey => Format is not SshKeyFormat.OpenSSH;

    /// <summary>
    ///     Exports the text representation of the SSH key.
    /// </summary>
    /// <returns>The text representation of the SSH key.</returns>
    public override string ExportTextOfKey()
    {
        return this is ISshPublicKey ? ExportOpenSshPublicKey()! : ExportOpenSshPrivateKey()!;
    }

    /// <summary>
    ///     Reads the contents of an SSH file.
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