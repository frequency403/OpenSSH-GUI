#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:24

#endregion

namespace OpenSSH_GUI.Core.Enums;

/// <summary>
/// Enumeration of SSH configuration files.
/// </summary>
public enum SshConfigFiles
{
    // ReSharper disable InconsistentNaming
    Authorized_Keys,

    /// <summary>
    /// Represents the Known_Hosts SSH config file.
    /// </summary>
    Known_Hosts,

    /// <summary>
    /// Enumerates the SSH configuration files.
    /// </summary>
    Config,

    /// <summary>
    /// Represents the Sshd_Config option of the SshConfigFiles enumeration.
    /// </summary>
    Sshd_Config
}