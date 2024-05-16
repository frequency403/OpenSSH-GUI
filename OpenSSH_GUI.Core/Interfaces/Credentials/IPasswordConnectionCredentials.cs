#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:35

#endregion

namespace OpenSSH_GUI.Core.Interfaces.Credentials;

/// <summary>
/// Represents the interface for password-based connection credentials.
/// </summary>
public interface IPasswordConnectionCredentials : IConnectionCredentials
{
    /// <summary>
    /// Represents the password connection credentials.
    /// </summary>
    string Password { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the password is encrypted.
    /// </summary>
    bool EncryptedPassword { get; set; }
}