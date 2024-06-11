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

/// *MultiKeyConnectionCredentials(string hostname, string username,
/// <see cref="IEnumerable{T}" />
/// ? keys)**
public class MultiKeyConnectionCredentials : ConnectionCredentials, IMultiKeyConnectionCredentials
{
    /// <summary>
    ///     Represents a set of connection credentials for a multi-key authentication.
    /// </summary>
    public MultiKeyConnectionCredentials(string hostname, string username, IEnumerable<ISshKey>? keys) : base(hostname,
        username, AuthType.MultiKey)
    {
        Keys = keys;
    }

    /// <summary>
    ///     Represents the credentials for a multi-key SSH connection.
    /// </summary>
    [JsonIgnore]
    public IEnumerable<ISshKey>? Keys { get; set; }


    /// <summary>
    ///     Retrieves the connection information for establishing an SSH connection.
    /// </summary>
    /// <returns>
    ///     The <see cref="ConnectionInfo" /> object representing the SSH connection information.
    /// </returns>
    public override ConnectionInfo GetConnectionInfo()
    {
        return new PrivateKeyConnectionInfo(Hostname, Port, Username, Keys.Select(e => e.GetSshNetKeyType()).ToArray());
    }
}