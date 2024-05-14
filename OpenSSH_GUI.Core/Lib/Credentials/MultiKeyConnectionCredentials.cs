#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 14.05.2024 - 00:05:30
// Last edit: 14.05.2024 - 03:05:40

#endregion

using System.Text.Json.Serialization;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Interfaces.Credentials;
using OpenSSH_GUI.Core.Interfaces.Keys;
using Renci.SshNet;

namespace OpenSSH_GUI.Core.Lib.Credentials;

public class MultiKeyConnectionCredentials(string hostname, string username, IEnumerable<ISshKey>? keys) : ConnectionCredentials(hostname, username, AuthType.MultiKey), IMultiKeyConnectionCredentials
{
    [JsonIgnore]
    public IEnumerable<ISshKey>? Keys { get; set; } = keys;

    public override ConnectionInfo GetConnectionInfo()
    {
        return new PrivateKeyConnectionInfo(Hostname, Port, Username, Keys.Select(e => e.GetRenciKeyType()).ToArray());
    }
}