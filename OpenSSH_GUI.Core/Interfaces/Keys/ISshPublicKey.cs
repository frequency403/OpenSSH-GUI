#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:33

#endregion

namespace OpenSSH_GUI.Core.Interfaces.Keys;

/// <summary>
/// Represents a SSH public key.
/// </summary>
public interface ISshPublicKey : ISshKey
{
    /// <summary>
    /// Represents the private key associated with an SSH public key.
    /// </summary>
    ISshKey PrivateKey { get; }
}