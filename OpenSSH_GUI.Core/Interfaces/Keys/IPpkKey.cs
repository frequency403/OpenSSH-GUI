#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:32

#endregion

using OpenSSH_GUI.Core.Enums;

namespace OpenSSH_GUI.Core.Interfaces.Keys;

/// <summary>
///     Represents an interface for a PpkKey.
/// </summary>
public interface IPpkKey : ISshKey
{
    /// <summary>
    ///     Represents the encryption type of an SSH key.
    /// </summary>
    EncryptionType EncryptionType { get; }

    /// <summary>
    ///     Gets the public key string of the SSH key.
    /// </summary>
    /// <remarks>
    ///     This property represents the public key string of the SSH key.
    /// </remarks>
    string PublicKeyString { get; }

    /// <summary>
    ///     Represents the private key string of an SSH key.
    /// </summary>
    /// <value>
    ///     The private key string.
    /// </value>
    string PrivateKeyString { get; }

    /// <summary>
    ///     Represents a PPK key.
    /// </summary>
    string PrivateMAC { get; }
}