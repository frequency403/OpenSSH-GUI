#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:24

#endregion

namespace OpenSSH_GUI.Core.Enums;

/// <summary>
///     The available encryption types for SSH keys.
/// </summary>
public enum EncryptionType
{
    /// <summary>
    ///     Represents the NONE member of the EncryptionType enum.
    /// </summary>
    NONE,

    /// <summary>
    ///     Represents the RSA encryption type.
    /// </summary>
    RSA,

    /// <summary>
    ///     Enumeration member representing the DSA encryption type.
    /// </summary>
    DSA,

    /// <summary>
    ///     Represents the encryption type for ECDSA.
    /// </summary>
    /// <remarks>
    ///     The ECDSA encryption type is used in the OpenSSH_GUI.Core library for representing ECDSA keys.
    /// </remarks>
    ECDSA,

    /// <summary>
    ///     The ED25519 encryption type.
    /// </summary>
    ED25519
}