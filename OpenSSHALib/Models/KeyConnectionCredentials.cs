#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 08.05.2024 - 22:05:30
// Last edit: 08.05.2024 - 22:05:55

#endregion

using OpenSSHALib.Interfaces;
using Renci.SshNet;

namespace OpenSSHALib.Models;

public class KeyConnectionCredentials : ConnectionCredentials, IKeyConnectionCredentials
{
    public ISshKey PublicKey { get; init; }

    public override ConnectionInfo GetConnectionInfo()
    {
        return new PrivateKeyConnectionInfo(Hostname, Username, ProxyTypes.None, "", 0, PublicKey.GetRenciKeyType());
    }
}