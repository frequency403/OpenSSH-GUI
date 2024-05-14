#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 08.05.2024 - 22:05:30
// Last edit: 14.05.2024 - 03:05:20

#endregion

using OpenSSH_GUI.Core.Interfaces.Credentials;
using OpenSSH_GUI.Core.Interfaces.Keys;
using Renci.SshNet;

namespace OpenSSH_GUI.Core.Models;

public class KeyConnectionCredentials : ConnectionCredentials, IKeyConnectionCredentials
{
    public ISshKey PublicKey { get; init; }

    public override ConnectionInfo GetConnectionInfo()
    {
        return new PrivateKeyConnectionInfo(Hostname, Username, ProxyTypes.None, "", 0, PublicKey.GetRenciKeyType());
    }
}