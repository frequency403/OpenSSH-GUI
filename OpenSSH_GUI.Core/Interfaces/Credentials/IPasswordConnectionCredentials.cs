namespace OpenSSH_GUI.Core.Interfaces.Credentials;

/// <summary>
///     Represents the interface for password-based connection credentials.
/// </summary>
public interface IPasswordConnectionCredentials : IConnectionCredentials
{
    /// <summary>
    ///     Represents the password connection credentials.
    /// </summary>
    string Password { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the password is encrypted.
    /// </summary>
    bool EncryptedPassword { get; set; }
}