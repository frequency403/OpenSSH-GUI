#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 08.05.2024 - 22:05:30
// Last edit: 14.05.2024 - 03:05:19

#endregion

using System.ComponentModel.DataAnnotations;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Interfaces.Credentials;
using Renci.SshNet;

namespace OpenSSH_GUI.Core.Lib.Credentials;

public class PasswordConnectionCredentials(string hostname, string username, string password, bool encryptedPassword = false) : ConnectionCredentials(hostname, username, AuthType.Password), IPasswordConnectionCredentials
{
    public string Password { get; set; } = password;
    public bool EncryptedPassword { get; set; } = encryptedPassword;
    
    public override ConnectionInfo GetConnectionInfo()
    {
        return new PasswordConnectionInfo(Hostname, Username, Password);
    }
}