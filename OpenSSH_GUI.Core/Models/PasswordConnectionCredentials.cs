#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 08.05.2024 - 22:05:30
// Last edit: 14.05.2024 - 03:05:19

#endregion

using OpenSSH_GUI.Core.Interfaces.Credentials;
using Renci.SshNet;

namespace OpenSSH_GUI.Core.Models;

public class PasswordConnectionCredentials : ConnectionCredentials, IPasswordConnectionCredentials
{
    public string Password { get; init; }

    public override ConnectionInfo GetConnectionInfo()
    {
        return new PasswordConnectionInfo(Hostname, Username, Password);
    }
}