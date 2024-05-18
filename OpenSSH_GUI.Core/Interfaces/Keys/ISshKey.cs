#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:33

#endregion

using OpenSSH_GUI.Core.Interfaces.Misc;

namespace OpenSSH_GUI.Core.Interfaces.Keys;

/// <summary>
/// Represents an SSH key.
/// </summary>
public interface ISshKey : IKeyBase
{
    int Id { get; set; }
    /// <summary>
    /// Gets the type of the SSH key as a string.
    /// </summary>
    string KeyTypeString { get; }

    /// <summary>
    /// Represents a comment associated with an SSH key.
    /// </summary>
    string Comment { get; }

    /// <summary>
    /// Gets a boolean value indicating whether the key is a public key.
    /// </summary>
    /// <value><c>true</c> if the key is a public key; otherwise, <c>false</c>.</value>
    bool IsPublicKey { get; }

    /// <summary>
    /// Gets a value indicating whether the SSH key is a Putty key.
    /// </summary>
    /// <remarks>
    /// The SSH key is considered a Putty key if its format is not OpenSSH.
    /// </remarks>
    bool IsPuttyKey { get; }

    /// <summary>
    /// Represents an SSH key type.
    /// </summary>
    ISshKeyType KeyType { get; }
}