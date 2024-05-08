#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 08.05.2024 - 22:05:30
// Last edit: 08.05.2024 - 22:05:55

#endregion

using OpenSSHALib.Interfaces;
using Renci.SshNet;

namespace OpenSSHALib.Models;

[Serializable]
public abstract class ConnectionCredentials : IConnectionCredentials
{
    public string Hostname { get; init; }
    public string Username { get; init; }
    public abstract ConnectionInfo GetConnectionInfo();
}