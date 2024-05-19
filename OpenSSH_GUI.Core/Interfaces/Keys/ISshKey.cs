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
    /// <summary>
    /// Represents the unique identifier for an SSH key.
    /// </summary>
    int Id { get; set; }

    /// <summary>
    /// Represents a comment associated with an SSH key.
    /// </summary>
    string Comment { get; }

    /// <summary>
    /// Gets a value indicating whether the SSH key is a Putty key.
    /// </summary>
    /// <remarks>
    /// The SSH key is considered a Putty key if its format is not OpenSSH.
    /// </remarks>
    bool IsPuttyKey { get; }
}