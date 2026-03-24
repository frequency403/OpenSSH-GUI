namespace OpenSSH_GUI.Core.Enums;

/// <summary>
///     Enumeration of SSH configuration files.
/// </summary>
public enum SshConfigFiles
{
    // ReSharper disable InconsistentNaming
    Authorized_Keys,

    /// <summary>
    ///     Represents the Known_Hosts SSH config file.
    /// </summary>
    Known_Hosts,

    /// <summary>
    ///     Enumerates the SSH configuration files.
    /// </summary>
    Config,

    /// <summary>
    ///     Represents the Sshd_Config option of the SshConfigFiles enumeration.
    /// </summary>
    Sshd_Config
}