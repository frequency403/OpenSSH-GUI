#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 14.05.2024 - 00:05:30
// Last edit: 14.05.2024 - 03:05:28

#endregion

using Renci.SshNet;

namespace OpenSSH_GUI.Core.Interfaces.Credentials;

public interface IConnectionCredentials
{
    string Hostname { get; init; }
    int Port { get; }
    string Username { get; init; }
    ConnectionInfo GetConnectionInfo();
}