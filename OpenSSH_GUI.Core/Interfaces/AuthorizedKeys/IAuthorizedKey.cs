#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:35

#endregion

using OpenSSH_GUI.Core.Enums;

namespace OpenSSH_GUI.Core.Interfaces.AuthorizedKeys;

/// <summary>
/// Represents an authorized key.
/// </summary>
public interface IAuthorizedKey
{
    /// <summary>
    /// Enumeration for SSH key types.
    /// </summary>
    KeyType KeyType { get; }

    /// <summary>
    /// Represents an authorized key entry in an authorized keys file.
    /// </summary>
    string Fingerprint { get; }

    /// <summary>
    /// Represents an authorized key entry in an authorized keys file.
    /// </summary>
    string Comment { get; }

    /// *Property: MarkedForDeletion**
    bool MarkedForDeletion { get; set; }

    /// <summary>
    /// Returns the full key entry of an authorized key.
    /// </summary>
    /// <returns>The full key entry string.</returns>
    string GetFullKeyEntry { get; }
}