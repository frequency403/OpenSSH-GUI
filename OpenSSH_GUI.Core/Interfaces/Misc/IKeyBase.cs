#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 01:05:51
// Last edit: 15.05.2024 - 01:05:48

#endregion

using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Database.DTO;
using OpenSSH_GUI.Core.Interfaces.AuthorizedKeys;
using OpenSSH_GUI.Core.Interfaces.Keys;
using Renci.SshNet;
using SshNet.Keygen;

namespace OpenSSH_GUI.Core.Interfaces.Misc;

/// <summary>
/// Represents the base interface for SSH keys.
/// </summary>
public interface IKeyBase
{
    /// <summary>
    /// Gets a value indicating whether the SSH key has a password.
    /// </summary>
    bool HasPassword { get; }

    /// <summary>
    /// Gets a value indicating whether the key requires a password.
    /// </summary>
    /// <remarks>
    /// This property is calculated based on the <see cref="HasPassword"/> and <see cref="PasswordSuccess"/> properties.
    /// If the key has a password and the password has not been successfully provided, then this property will be true.
    /// </remarks>
    bool NeedPassword { get; }

    /// <summary>
    /// Gets or sets a value indicating whether the password authentication was successful for the SSH key.
    /// </summary>
    bool PasswordSuccess { get; set; }

    /// <summary>
    /// Represents a password associated with a key.
    /// </summary>
    string? Password { get; set; }

    /// <summary>
    /// Gets the absolute file path of the SSH key.
    /// </summary>
    /// <returns>The absolute file path of the SSH key.</returns>
    string AbsoluteFilePath { get; }

    /// <summary>
    /// Represents a fingerprint of a key.
    /// </summary>
    string Fingerprint { get; }

    /// <summary>
    /// Gets the filename of the key.
    /// </summary>
    string Filename { get; }

    /// Gets the format of the SSH key.
    /// @return The format of the SSH key.
    /// /
    SshKeyFormat Format { get; }

    /// <summary>
    /// Represents the key type for an SSH key.
    /// </summary>
    ISshKeyType KeyType { get; }

    /// <summary>
    /// Export the public key in OpenSSH format.
    /// </summary>
    /// <returns>The public key in OpenSSH format.</returns>
    string? ExportOpenSshPublicKey();

    /// <summary>
    /// Exports the private key in OpenSSH format.
    /// </summary>
    /// <returns>The private key in OpenSSH format as a string, or null if the key source is null.</returns>
    string? ExportOpenSshPrivateKey();

    /// <summary>
    /// Exports the public key in PuTTY format.
    /// </summary>
    /// <returns>The public key in PuTTY format.</returns>
    string? ExportPuttyPublicKey();

    /// <summary>
    /// Exports the Putty PPK key format of the SSH key.
    /// </summary>
    /// <returns>
    /// The Putty PPK key format as a string.
    /// </returns>
    string? ExportPuttyPpkKey();

    /// <summary>
    /// Exports the text representation of the key.
    /// </summary>
    /// <returns>The text representation of the key.</returns>
    string? ExportTextOfKey();

    /// <summary>
    /// Exports the authorized key entry for the key.
    /// </summary>
    /// <returns>The authorized key entry for the key.</returns>
    string? ExportAuthorizedKeyEntry();

    /// <summary>
    /// Exports the authorized key as an instance of IAuthorizedKey.
    /// </summary>
    /// <returns>The exported authorized key.</returns>
    IAuthorizedKey ExportAuthorizedKey();

    /// <summary>
    /// Retrieves the SSH.NET key type of the key.
    /// </summary>
    /// <returns>The SSH.NET key type of the key.</returns>
    IPrivateKeySource? GetSshNetKeyType();

    /// <summary>
    /// Deletes the SSH key.
    /// </summary>
    void DeleteKey();

    /// <summary>
    /// Converts the current instance of the <see cref="KeyBase"/> class to an instance of <see cref="SshKeyDto"/>.
    /// </summary>
    /// <returns>An instance of <see cref="SshKeyDto"/>.</returns>
    SshKeyDto ToDto();
}