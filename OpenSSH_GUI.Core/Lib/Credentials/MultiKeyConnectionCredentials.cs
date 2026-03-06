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
        username, AuthType.MultiKey)
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
        return new PrivateKeyConnectionInfo(Hostname, Port, Username, Keys?.Select(e => e.PrivateKeySource).ToArray());
    }
}