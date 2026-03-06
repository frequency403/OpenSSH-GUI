using System.ComponentModel.DataAnnotations;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Interfaces.Credentials;
using OpenSSH_GUI.Core.Lib.Credentials;
using OpenSSH_GUI.Core.Lib.Keys;

namespace OpenSSH_GUI.Core.Database.DTO;

/// <summary>
///     Represents a data transfer object (DTO) for connection credentials.
/// </summary>
public class ConnectionCredentialsDto
{
    /// <summary>
    ///     Represents the property Id of a connection credential.
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    ///     Represents the hostname of a connection.
    /// </summary>
    [Encrypted]
    public string Hostname { get; set; }

    /// <summary>
    ///     Represents the username used for connection credentials.
    /// </summary>
    [Encrypted]
    public string Username { get; set; }

    /// <summary>
    ///     The port number for the SSH connection.
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    ///     Represents the authentication type for a connection.
    /// </summary>
    public AuthType AuthType { get; set; }

    /// <summary>
    ///     Represents a data transfer object for connection credentials.
    /// </summary>
    public virtual List<SshKeyDto> KeyDtos { get; set; }

    /// <summary>
    ///     Represents the password for a connection.
    /// </summary>
    [Encrypted]
    public string? Password { get; set; }

    /// <summary>
    ///     Gets or sets a flag indicating whether the password is encrypted.
    /// </summary>
    public bool PasswordEncrypted { get; set; }

    /// <summary>
    ///     Converts a ConnectionCredentialsDto object to an instance of IConnectionCredentials.
    /// </summary>
    /// <returns>The converted IConnectionCredentials object.</returns>
    public async ValueTask<IConnectionCredentials> ToCredentials()
    {
        var g = KeyDtos.Select(async e => await e.ToKey()).ToArray();
        await Task.WhenAll(g);
        return await ToCredentials(g.Select(e => e.Result));
    }


    public async ValueTask<IConnectionCredentials> ToCredentials(params IEnumerable<SshKeyFile> keys)
    {
        return AuthType switch
        {
            AuthType.Key => new KeyConnectionCredentials(Hostname, Username, await KeyDtos.First().ToKey()) { Id = Id },
            AuthType.Password => new PasswordConnectionCredentials(Hostname, Username, Password,
                PasswordEncrypted) { Id = Id },
            AuthType.MultiKey => new MultiKeyConnectionCredentials(Hostname, Username,
                keys.Where(e => KeyDtos.All(f => f.AbsolutePath != e.AbsoluteFilePath))) { Id = Id }
        };
    }
}