using System.Text.Json.Serialization;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Interfaces.Credentials;
using OpenSSH_GUI.Core.Lib.Keys;
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
    public MultiKeyConnectionCredentials(string hostname, string username, IEnumerable<SshKeyFile>? keys) : base(
        hostname,
        username)
    {
        Keys = keys;
    }

    /// <summary>
    ///     Represents the credentials for a multi-key SSH connection.
    /// </summary>
    [JsonIgnore]
    public IEnumerable<SshKeyFile>? Keys { get; set; }


    /// <summary>
    ///     Retrieves the connection information for establishing an SSH connection.
    /// </summary>
    /// <returns>
    ///     The <see cref="ConnectionInfo" /> object representing the SSH connection information.
    /// </returns>
    public override ConnectionInfo GetConnectionInfo()
    {
        if (Keys is not { } keys) return new ConnectionInfo(Hostname, Port, Username);
        var sources = keys.Select(e => e.PrivateKeyFile).ToArray();
        return sources.All(s => s is not null) ? new PrivateKeyConnectionInfo(Hostname, Port, Username, sources as PrivateKeyFile[]) : new ConnectionInfo(Hostname, Port, Username);
    }
}