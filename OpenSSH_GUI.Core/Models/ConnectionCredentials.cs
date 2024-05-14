#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 14.05.2024 - 00:05:30
// Last edit: 14.05.2024 - 03:05:20

#endregion

using OpenSSH_GUI.Core.Interfaces.Credentials;
using Renci.SshNet;

namespace OpenSSH_GUI.Core.Models;

[Serializable]
public abstract class ConnectionCredentials : IConnectionCredentials
{
    public string Hostname { get; init; }
    public int Port => Hostname.Contains(':') ? int.Parse(Hostname.Split(':')[1]) : 22;
    public string Username { get; init; }
    public abstract ConnectionInfo GetConnectionInfo();
}