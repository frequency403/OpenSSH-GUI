#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:33

#endregion

using OpenSSH_GUI.Core.Enums;

namespace OpenSSH_GUI.Core.Interfaces.Keys;

/// <summary>
/// Represents an SSH key type.
/// </summary>
public interface ISshKeyType
{
    /// <summary>
    /// Interface for SSH key types.
    /// </summary>
    KeyType BaseType { get; }

    /// <summary>
    /// Represents an SSH key type.
    /// </summary>
    string KeyTypeText { get; }

    /// <summary>
    /// Specifies whether the SSH key type has a default bit size.
    /// </summary>
    bool HasDefaultBitSize { get; }

    /// <summary>
    /// Gets or sets the current bit size of the SSH key type.
    /// </summary>
    int CurrentBitSize { get; set; }

    /// <summary>
    /// Represents a property that provides possible bit sizes for SSH key types.
    /// </summary>
    IEnumerable<int> PossibleBitSizes { get; }
}