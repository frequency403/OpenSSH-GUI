using System.Text.Json.Serialization;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Interfaces.Credentials;
using OpenSSH_GUI.Core.Lib.Keys;
using Renci.SshNet;

namespace OpenSSH_GUI.Core.Lib.Credentials;

/// <summary>
///     Represents the credentials for a key-based connection to a server.
/// </summary>
public class KeyConnectionCredentials : ConnectionCredentials, IKeyConnectionCredentials
{
    /// <summary>
    ///     Represents connection credentials using SSH key authentication.
    /// </summary>
    public KeyConnectionCredentials(string hostname, string username, SshKeyFile? key) : base(hostname, username)
    {
        Key = key;
    }

    /// <summary>
    ///     Represents connection credentials that include an SSH key for authentication.
    /// </summary>
    [JsonIgnore]
    public SshKeyFile? Key { get; set; }


    /// <summary>
    ///     Renews the SSH key used for authentication.
    /// </summary>
    /// <param name="password">The password for the key file (optional).</param>
    public void RenewKey(string? password = null)
    {
    }

    /// <summary>
    ///     Retrieves the connection information based on the provided credentials.
    /// </summary>
    /// <returns>
    ///     The <see cref="ConnectionInfo" /> object representing the SSH connection information.
    /// </returns>
    public override ConnectionInfo GetConnectionInfo()
    {
        return new PrivateKeyConnectionInfo(Hostname, Username, ProxyTypes.None, "", 0, Key?.PrivateKeySource);
    }
}