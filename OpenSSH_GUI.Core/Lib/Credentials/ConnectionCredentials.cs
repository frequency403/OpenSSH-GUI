#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:31

#endregion

using System.Text.Json.Serialization;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Interfaces.Credentials;
using Renci.SshNet;

namespace OpenSSH_GUI.Core.Lib.Credentials;

// [JsonPolymorphic(UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToNearestAncestor)]
// [JsonDerivedType(typeof(PasswordConnectionCredentials), nameof(PasswordConnectionCredentials))]
// [JsonDerivedType(typeof(KeyConnectionCredentials), nameof(KeyConnectionCredentials))]
// [JsonDerivedType(typeof(MultiKeyConnectionCredentials), nameof(MultiKeyConnectionCredentials))]
public abstract class ConnectionCredentials(string hostname, string username, AuthType authType)
    : IConnectionCredentials
{
    public string Hostname { get; set; } = hostname;
    public int Port => Hostname.Contains(':') ? int.Parse(Hostname.Split(':')[1]) : 22;
    public string Username { get; set; } = username;
    public abstract ConnectionInfo GetConnectionInfo();

    [JsonIgnore] public string Display => ToString();

    public AuthType AuthType { get; } = authType;

    public override string ToString()
    {
        return $"{Username}@{Hostname}{(Port is 22 ? "" : $":{Port}")}";
    }
}