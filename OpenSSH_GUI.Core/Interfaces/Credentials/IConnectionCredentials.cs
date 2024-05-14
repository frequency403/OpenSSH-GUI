#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 14.05.2024 - 00:05:30
// Last edit: 14.05.2024 - 03:05:28

#endregion

using System.Text.Json.Serialization;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Lib.Credentials;
using Renci.SshNet;

namespace OpenSSH_GUI.Core.Interfaces.Credentials;

// [JsonPolymorphic(UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToNearestAncestor)]
// [JsonDerivedType(typeof(IPasswordConnectionCredentials), nameof(PasswordConnectionCredentials))]
// [JsonDerivedType(typeof(IKeyConnectionCredentials), nameof(KeyConnectionCredentials))]
// [JsonDerivedType(typeof(IMultiKeyConnectionCredentials), nameof(MultiKeyConnectionCredentials))]
public interface IConnectionCredentials
{
    string Hostname { get; set; }
    int Port { get; }
    string Username { get; set; }
    [JsonIgnore]
    string Display { get; }
    AuthType AuthType { get; }
    ConnectionInfo GetConnectionInfo();
}