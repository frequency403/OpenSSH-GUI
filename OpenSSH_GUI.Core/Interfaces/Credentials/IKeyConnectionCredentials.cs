using System.Text.Json.Serialization;
using OpenSSH_GUI.Core.Lib.Keys;

namespace OpenSSH_GUI.Core.Interfaces.Credentials;

/// <summary>
///     Represents connection credentials for SSH using key-based authentication.
/// </summary>
public interface IKeyConnectionCredentials : IConnectionCredentials
{
    /// <summary>
    ///     Represents a connection credential that includes an SSH key.
    /// </summary>
    [JsonIgnore]
    SshKeyFile? Key { get; set; }

    /// <summary>
    ///     Renews the SSH key used for authentication.
    /// </summary>
    /// <param name="password">The password for the key file (optional).</param>
    void RenewKey(string? password = null);
}