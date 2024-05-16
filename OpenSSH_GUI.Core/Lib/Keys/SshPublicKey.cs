#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:27

#endregion

using OpenSSH_GUI.Core.Interfaces.Keys;

namespace OpenSSH_GUI.Core.Lib.Keys;

/// <summary>
/// Represents an SSH public key.
/// </summary>
public class SshPublicKey(string absoluteFilePath, string? password = null) : SshKey(absoluteFilePath, password), ISshPublicKey
{
    /// <summary>
    /// Represents a private key.
    /// </summary>
    public ISshKey PrivateKey { get; } = new SshPrivateKey(Path.ChangeExtension(absoluteFilePath, null), password);
}