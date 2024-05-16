#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:30

#endregion

using System.Text.Json.Serialization;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Interfaces.Credentials;
using OpenSSH_GUI.Core.Interfaces.Keys;
using OpenSSH_GUI.Core.Lib.Keys;
using Renci.SshNet;

namespace OpenSSH_GUI.Core.Lib.Credentials;

/// <summary>
/// Represents the credentials for a key-based connection to a server.
/// </summary>
public class KeyConnectionCredentials : ConnectionCredentials, IKeyConnectionCredentials
{
    /// <summary>
    /// Represents connection credentials using SSH key authentication.
    /// </summary>
    public KeyConnectionCredentials(string hostname, string username, ISshKey? key) : base(hostname, username, AuthType.Key)
    {
        Key = key;
        KeyPassword = Key?.Password;
    }

    /// <summary>
    /// Represents connection credentials that include an SSH key for authentication.
    /// </summary>
    [JsonIgnore] public ISshKey? Key { get; set; }

    /// <summary>
    /// Gets the file path of the key used for SSH connection authentication.
    /// </summary>
    public string KeyFilePath => Key?.AbsoluteFilePath ?? "";

    /// <summary>
    /// Gets or sets the password for the SSH key.
    /// </summary>
    /// <remarks>
    /// The password is used to unlock the private key when connecting via SSH. It is optional and can be null or empty.
    /// </remarks>
    public string? KeyPassword { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the password is encrypted.
    /// </summary>
    public bool PasswordEncrypted { get; set; }

    /// <summary>
    /// Renews the SSH key used for authentication.
    /// </summary>
    /// <param name="password">The password for the key file (optional).</param>
    public void RenewKey(string? password = null)
    {
        KeyPassword = password;
        Key = Path.GetExtension(KeyFilePath) switch
        {
            var x when x.Contains("pub") => new SshPublicKey(KeyFilePath, KeyPassword),
            var x when x.Contains("ppk") => new PpkKey(KeyFilePath, KeyPassword),
            var x when string.IsNullOrWhiteSpace(x) => Key,
            _ => new SshPrivateKey(KeyFilePath, KeyPassword)
        };
    }

    /// <summary>
    /// Retrieves the connection information based on the provided credentials.
    /// </summary>
    /// <returns>
    /// The <see cref="ConnectionInfo"/> object representing the SSH connection information.
    /// </returns>
    public override ConnectionInfo GetConnectionInfo()
    {
        return new PrivateKeyConnectionInfo(Hostname, Username, ProxyTypes.None, "", 0, Key.GetSshNetKeyType());
    }
}