// File Created by: Oliver Schantz
// Created: 18.05.2024 - 16:05:59
// Last edit: 18.05.2024 - 16:05:59

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Interfaces.Credentials;
using OpenSSH_GUI.Core.Lib.Credentials;
using OpenSSH_GUI.Core.Lib.Settings;
using OpenSSH_GUI.Core.Lib.Static;

namespace OpenSSH_GUI.Core.Database.DTO;

/// <summary>
/// Represents a data transfer object (DTO) for connection credentials.
/// </summary>
public class ConnectionCredentialsDto
{
    /// <summary>
    /// Represents the property Id of a connection credential.
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Represents the hostname of a connection.
    /// </summary>
    [Encrypted]
    public string Hostname { get; set; }

    /// <summary>
    /// Represents the username used for connection credentials.
    /// </summary>
    [Encrypted]
    public string Username { get; set; }

    /// <summary>
    /// The port number for the SSH connection.
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// Represents the authentication type for a connection.
    /// </summary>
    public AuthType AuthType { get; set; }

    /// <summary>
    /// Represents a data transfer object for connection credentials.
    /// </summary>
    public virtual List<SshKeyDto> KeyDtos { get; set; }

    /// <summary>
    /// Represents the password for a connection.
    /// </summary>
    [Encrypted]
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets a flag indicating whether the password is encrypted.
    /// </summary>
    public bool PasswordEncrypted { get; set; }

    /// <summary>
    /// Converts a ConnectionCredentialsDto object to an instance of IConnectionCredentials.
    /// </summary>
    /// <returns>The converted IConnectionCredentials object.</returns>
    public IConnectionCredentials ToCredentials()
    {
        return AuthType switch
        {
            AuthType.Key => new KeyConnectionCredentials(Hostname, Username, KeyDtos.First().ToKey()) {Id = this.Id},
            AuthType.Password => new PasswordConnectionCredentials(Hostname, Username, Password,
                PasswordEncrypted) {Id = this.Id},
            AuthType.MultiKey => new MultiKeyConnectionCredentials(Hostname, Username, KeyDtos.Select(e => e.ToKey())) {Id = this.Id}
        };
    }
}