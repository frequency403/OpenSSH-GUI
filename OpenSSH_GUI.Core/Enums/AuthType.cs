#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:24

#endregion

namespace OpenSSH_GUI.Core.Enums;

/// <summary>
/// Represents the types of authentication supported for SSH connections.
/// </summary>
public enum AuthType
{
    /// <summary>
    /// Represents connection credentials using password authentication.
    /// </summary>
    Password,

    /// <summary>
    /// Represents the authentication type of connection credentials using SSH key.
    /// </summary>
    Key,

    /// <summary>
    /// Represents a multi-key authentication type for SSH connections.
    /// </summary>
    MultiKey
}