#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:24

#endregion

namespace OpenSSH_GUI.Core.Enums;

/// <summary>
/// Enumeration for SSH key types.
/// </summary>
public enum KeyType
{
    // ReSharper disable InconsistentNaming
    /// <summary>
    /// RSA key type.
    /// </summary>
    RSA,

    /// <summary>
    /// Represents the ECDSA key type.
    /// </summary>
    ECDSA,

    /// <summary>
    /// Represents the ED25519 key type.
    /// </summary>
    ED25519
}