#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 08.05.2024 - 22:05:30
// Last edit: 08.05.2024 - 22:05:53

#endregion

using Renci.SshNet;

namespace OpenSSHALib.Interfaces;

public interface IConnectionCredentials
{
    string Hostname { get; init; }
    string Username { get; init; }
    ConnectionInfo GetConnectionInfo();
}