#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:31

#endregion

using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Interfaces.Credentials;
using Renci.SshNet;

namespace OpenSSH_GUI.Core.Lib.Credentials;

/// **hostname**: The hostname or IP address of the remote server.
/// **username**: The username used to authenticate with the remote server.
/// **password**: The password used to authenticate with the remote server.
/// **encryptedPassword**: (optional) Specifies whether the password is encrypted. The default value is `false`.
public class PasswordConnectionCredentials(
    string hostname,
    string username,
    string password,
    bool encryptedPassword = false)
    : ConnectionCredentials(hostname, username, AuthType.Password), IPasswordConnectionCredentials
{
    /// <summary>
    ///     Represents connection credentials using password authentication.
    /// </summary>
    public string Password { get; set; } = password;

    /// <summary>
    ///     Gets or sets a value indicating whether the password is encrypted.
    /// </summary>
    /// <value>
    ///     <c>true</c> if the password is encrypted; otherwise, <c>false</c>.
    /// </value>
    public bool EncryptedPassword { get; set; } = encryptedPassword;

    /// <summary>
    ///     Retrieves the connection information based on the provided credentials.
    /// </summary>
    /// <returns>The <see cref="ConnectionInfo" /> object representing the SSH connection information.</returns>
    public override ConnectionInfo GetConnectionInfo()
    {
        return new PasswordConnectionInfo(Hostname, Username, Password);
    }
}