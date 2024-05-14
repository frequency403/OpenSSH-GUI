#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 08.05.2024 - 22:05:30
// Last edit: 14.05.2024 - 03:05:20

#endregion

using System.Text.Json.Serialization;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Interfaces.Credentials;
using OpenSSH_GUI.Core.Interfaces.Keys;
using OpenSSH_GUI.Core.Lib.Keys;
using Renci.SshNet;

namespace OpenSSH_GUI.Core.Lib.Credentials;

public class KeyConnectionCredentials(string hostname, string username, ISshKey? key) : ConnectionCredentials(hostname, username, AuthType.Key), IKeyConnectionCredentials
{
    [JsonIgnore]
    public ISshKey? Key { get; set; } = key;
    public string KeyFilePath { get; } = key?.AbsoluteFilePath ?? "";

    public void RenewKey()
    {
        Key = Path.GetExtension(KeyFilePath) switch
        {
            var x when x.Contains("pub") => new SshPublicKey(KeyFilePath),
            var x when x.Contains("ppk") => new PpkKey(KeyFilePath),
            var x when string.IsNullOrWhiteSpace(x) => new SshPrivateKey(KeyFilePath),
            _ => Key
        };
    }

    public override ConnectionInfo GetConnectionInfo()
    {
        return new PrivateKeyConnectionInfo(Hostname, Username, ProxyTypes.None, "", 0, Key.GetRenciKeyType());
    }
}