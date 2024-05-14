#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 14.05.2024 - 00:05:30
// Last edit: 14.05.2024 - 03:05:40

#endregion

using OpenSSH_GUI.Core.Interfaces.Credentials;
using OpenSSH_GUI.Core.Interfaces.Keys;
using Renci.SshNet;

namespace OpenSSH_GUI.Core.Models;

public class MultiKeyConnectionCredentials : ConnectionCredentials, IMultiKeyConnectionCredentials
{
    public IEnumerable<ISshKey> Keys { get; init; }

    public override ConnectionInfo GetConnectionInfo()
    {
        return new PrivateKeyConnectionInfo(Hostname, Port, Username, Keys.Select(e => e.GetRenciKeyType()).ToArray());
    }
}