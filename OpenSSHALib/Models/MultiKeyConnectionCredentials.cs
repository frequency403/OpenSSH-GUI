// File Created by: Oliver Schantz
// Created: 13.05.2024 - 15:05:47
// Last edit: 13.05.2024 - 15:05:47

using OpenSSHALib.Interfaces;
using Renci.SshNet;

namespace OpenSSHALib.Models;

public class MultiKeyConnectionCredentials : ConnectionCredentials, IMultiKeyConnectionCredentials
{
    public IEnumerable<ISshKey> Keys { get; init; }

    public override ConnectionInfo GetConnectionInfo()
    {
        return new PrivateKeyConnectionInfo(Hostname, Port ,Username, Keys.Select(e => e.GetRenciKeyType()).ToArray());
    }
}