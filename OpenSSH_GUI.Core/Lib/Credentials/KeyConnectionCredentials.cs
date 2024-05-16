#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:30

#endregion

using System.Text.Json.Serialization;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Interfaces.Credentials;
using OpenSSH_GUI.Core.Interfaces.Keys;
using OpenSSH_GUI.Core.Lib.Keys;
using Renci.SshNet;

namespace OpenSSH_GUI.Core.Lib.Credentials;

public class KeyConnectionCredentials : ConnectionCredentials, IKeyConnectionCredentials
{
    public KeyConnectionCredentials(string hostname, string username, ISshKey? key) : base(hostname, username, AuthType.Key)
    {
        Key = key;
        KeyPassword = Key?.Password;
    }
    
    [JsonIgnore] public ISshKey? Key { get; set; }

    public string KeyFilePath => Key?.AbsoluteFilePath ?? "";
    public string? KeyPassword { get; set; }
    public bool PasswordEncrypted { get; set; }
    
    public void RenewKey(string? password = null)
    {
        KeyPassword = password;
        Key = Path.GetExtension(KeyFilePath) switch
        {
            var x when x.Contains("pub") => new SshPublicKey(KeyFilePath, KeyPassword),
            var x when x.Contains("ppk") => new PpkKey(KeyFilePath, KeyPassword),
            var x when string.IsNullOrWhiteSpace(x) => Key,
            _ => new SshPrivateKey(KeyFilePath, KeyPassword)
        };
    }

    public override ConnectionInfo GetConnectionInfo()
    {
        return new PrivateKeyConnectionInfo(Hostname, Username, ProxyTypes.None, "", 0, Key.GetRenciKeyType());
    }
}