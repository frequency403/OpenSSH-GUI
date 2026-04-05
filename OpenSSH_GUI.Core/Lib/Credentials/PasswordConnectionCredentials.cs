using OpenSSH_GUI.Core.Interfaces.Credentials;
using Renci.SshNet;

namespace OpenSSH_GUI.Core.Lib.Credentials;

public class PasswordConnectionCredentials(
    string hostname,
    string username,
    string password,
    bool encryptedPassword = false)
    : ConnectionCredentials(hostname, username), IPasswordConnectionCredentials
{
    /// <summary>
    ///     Represents connection credentials using password authentication.
    /// </summary>
    public string Password { get; set; } = password;

    /// <summary>
    ///     Gets or sets a value indicating whether the password is encrypted.
    /// </summary>
    /// <value>
    ///     <c>true</c> if the password is encrypted; otherwise, <c>false</c>.
    /// </value>
    public bool EncryptedPassword { get; set; } = encryptedPassword;

    /// <summary>
    ///     Retrieves the connection information based on the provided credentials.
    /// </summary>
    /// <returns>The <see cref="ConnectionInfo" /> object representing the SSH connection information.</returns>
    public override ConnectionInfo GetConnectionInfo()
    {
        return new PasswordConnectionInfo(Hostname, Username, Password);
    }
}