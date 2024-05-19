// File Created by: Oliver Schantz
// Created: 17.05.2024 - 08:05:19
// Last edit: 17.05.2024 - 08:05:19

using System.ComponentModel.DataAnnotations;
using OpenSSH_GUI.Core.Interfaces.Keys;
using OpenSSH_GUI.Core.Lib.Static;
using SshNet.Keygen;

namespace OpenSSH_GUI.Core.Database.DTO;

/// <summary>
/// Represents a SSH key data transfer object (DTO).
/// </summary>
public class SshKeyDto
{
    /// <summary>
    /// Gets or sets the ID of the SSH key.
    /// </summary>
    /// <remarks>
    /// The ID uniquely identifies the SSH key.
    /// </remarks>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the absolute path of the SSH key.
    /// </summary>
    public string AbsolutePath { get; set; }

    /// <summary>
    /// Represents the format of an SSH key.
    /// </summary>
    public SshKeyFormat Format { get; set; }

    /// <summary>
    /// Represents a password associated with a specific SSH key.
    /// </summary>
    [Encrypted]
    public string? Password { get; set; }

    /// <summary>
    /// Represents the connection credentials for SSH connection.
    /// </summary>
    public virtual IEnumerable<ConnectionCredentialsDto> ConnectionCredentialsDto { get; set; }

    /// <summary>
    /// Converts an instance of <see cref="SshKeyDto"/> to an instance of <see cref="ISshKey"/>.
    /// </summary>
    /// <returns>The converted <see cref="ISshKey"/> instance.</returns>
    public ISshKey? ToKey()
    {
        return KeyFactory.FromPath(AbsolutePath, Password, Id);
    }
}