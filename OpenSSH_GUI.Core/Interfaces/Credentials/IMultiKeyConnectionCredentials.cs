using System.Text.Json.Serialization;
using OpenSSH_GUI.Core.Lib.Keys;

namespace OpenSSH_GUI.Core.Interfaces.Credentials;

/// <summary>
///     Represents the interface for multi-key connection credentials.
/// </summary>
public interface IMultiKeyConnectionCredentials : IConnectionCredentials
{
    /// <summary>
    ///     Represents the credentials for a multi-key connection.
    /// </summary>
    [JsonIgnore]
    IEnumerable<SshKeyFile>? Keys { get; set; }
}