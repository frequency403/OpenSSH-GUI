#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:31

#endregion

using System.Text.Json.Serialization;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Interfaces.Credentials;
using OpenSSH_GUI.Core.Interfaces.Keys;
using Renci.SshNet;

namespace OpenSSH_GUI.Core.Lib.Credentials;

public class MultiKeyConnectionCredentials : ConnectionCredentials, IMultiKeyConnectionCredentials
{
    public MultiKeyConnectionCredentials(string hostname, string username, IEnumerable<ISshKey>? keys) : base(hostname, username, AuthType.MultiKey)
    {
        Keys = keys;
        Passwords = Keys?.Select(e => new KeyValuePair<string, string?>(e.AbsoluteFilePath, e.Password)).ToDictionary();
    }
    
    [JsonIgnore] public IEnumerable<ISshKey>? Keys { get; set; }

    public Dictionary<string, string?>? Passwords { get; set; }
    public bool PasswordsEncrypted { get; set; }
    public override ConnectionInfo GetConnectionInfo()
    {
        return new PrivateKeyConnectionInfo(Hostname, Port, Username, Keys.Select(e => e.GetRenciKeyType()).ToArray());
    }
}