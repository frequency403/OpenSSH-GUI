#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:31

#endregion

using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Interfaces.Credentials;
using Renci.SshNet;

namespace OpenSSH_GUI.Core.Lib.Credentials;

public class PasswordConnectionCredentials(
    string hostname,
    string username,
    string password,
    bool encryptedPassword = false)
    : ConnectionCredentials(hostname, username, AuthType.Password), IPasswordConnectionCredentials
{
    public string Password { get; set; } = password;
    public bool EncryptedPassword { get; set; } = encryptedPassword;

    public override ConnectionInfo GetConnectionInfo()
    {
        return new PasswordConnectionInfo(Hostname, Username, Password);
    }
}