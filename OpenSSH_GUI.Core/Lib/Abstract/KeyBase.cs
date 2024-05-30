﻿#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:46
// Last edit: 15.05.2024 - 01:05:48

#endregion

using OpenSSH_GUI.Core.Database.Context;
using OpenSSH_GUI.Core.Database.DTO;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Interfaces.AuthorizedKeys;
using OpenSSH_GUI.Core.Interfaces.Keys;
using OpenSSH_GUI.Core.Interfaces.Misc;
using OpenSSH_GUI.Core.Lib.AuthorizedKeys;
using OpenSSH_GUI.Core.Lib.Static;
using Renci.SshNet;
using Renci.SshNet.Common;
using SshNet.Keygen;
using SshNet.Keygen.Extensions;
using SshNet.PuttyKeyFile;
using SshKeyType = OpenSSH_GUI.Core.Lib.Keys.SshKeyType;

namespace OpenSSH_GUI.Core.Lib.Abstract;

/// <summary>
///     Base class for SSH keys.
/// </summary>
public abstract class KeyBase : IKeyBase
{
    /// <summary>
    ///     Represents the source of a private key for SSH.
    /// </summary>
    private IPrivateKeySource? _keySource;

    /// <summary>
    ///     Base class for SSH keys.
    /// </summary>
    protected KeyBase(string absoluteFilePath, string? password = null)
    {
        AbsoluteFilePath = absoluteFilePath;
        Filename = Path.GetFileName(AbsoluteFilePath);
        Password = password;
        SetKeySource();
        if (NeedPassword) return;
        KeyType = new SshKeyType(_keySource.HostKeyAlgorithms.FirstOrDefault()?.Name);
        Fingerprint = _keySource.FingerprintHash();
    }

    /// <summary>
    ///     Represents the identifier of a SSH key.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    ///     Represents the type of SSH key.
    /// </summary>
    public ISshKeyType KeyType { get; }

    /// <summary>
    ///     Gets a value indicating whether the key requires a password.
    /// </summary>
    /// <value>
    ///     <c>true</c> if the key requires a password; otherwise, <c>false</c>.
    /// </value>
    public bool NeedPassword => HasPassword && !PasswordSuccess;

    /// <summary>
    ///     Represents the success status of a password for accessing the private key file.
    /// </summary>
    public bool PasswordSuccess { get; set; }

    /// <summary>
    ///     Indicates that the key is passwort protected or not
    /// </summary>
    public bool HasPassword => Password is not null;

    /// <summary>
    ///     Represents a password for a SSH key.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    ///     Gets the filename of the key file.
    /// </summary>
    public string Filename { get; }

    /// <summary>
    ///     Gets the absolute file path of the key.
    /// </summary>
    /// <value>
    ///     The absolute file path of the key.
    /// </value>
    public string AbsoluteFilePath { get; }

    /// <summary>
    ///     Gets the format of the key.
    /// </summary>
    public SshKeyFormat Format { get; protected init; }

    /// <summary>
    ///     Represents a fingerprint of a key.
    /// </summary>
    public string Fingerprint { get; protected set; }

    /// <summary>
    ///     Exports the authorized key entry for the key.
    /// </summary>
    /// <returns>The authorized key entry for the key.</returns>
    public string? ExportAuthorizedKeyEntry()
    {
        return ExportOpenSshPublicKey();
    }

    /// <summary>
    ///     Export the public key in OpenSSH format.
    /// </summary>
    /// <returns>The public key in OpenSSH format.</returns>
    public string? ExportOpenSshPublicKey()
    {
        return _keySource?.ToOpenSshPublicFormat();
    }

    /// <summary>
    ///     Exports the private key in OpenSSH format.
    /// </summary>
    /// <returns>The private key in OpenSSH format as a string, or null if the key source is null.</returns>
    public string? ExportOpenSshPrivateKey()
    {
        return _keySource?.ToOpenSshFormat();
    }

    /// <summary>
    ///     Exports the public key in PuTTY format.
    /// </summary>
    /// <returns>The public key in PuTTY format.</returns>
    public string? ExportPuttyPublicKey()
    {
        return _keySource?.ToPuttyPublicFormat();
    }

    /// <summary>
    ///     Exports the Putty PPK key format of the SSH key.
    /// </summary>
    /// <returns>The Putty PPK key format as a string.</returns>
    public string? ExportPuttyPpkKey()
    {
        return _keySource?.ToPuttyFormat();
    }

    /// <summary>
    ///     Exports the text representation of the key.
    /// </summary>
    /// <returns>The text representation of the key.</returns>
    public abstract string ExportTextOfKey();

    /// <summary>
    ///     Exports the authorized key as a string in OpenSSH format.
    /// </summary>
    /// <returns>The authorized key entry in OpenSSH format.</returns>
    public IAuthorizedKey ExportAuthorizedKey()
    {
        return new AuthorizedKey(ExportAuthorizedKeyEntry());
    }

    /// <summary>
    ///     Gets the SSH.NET key type of the key.
    /// </summary>
    /// <returns>The SSH.NET key type of the key.</returns>
    public IPrivateKeySource? GetSshNetKeyType()
    {
        return _keySource;
    }

    public SshKeyDto ToDto()
    {
        using var dbContext = new OpenSshGuiDbContext();
        var found = dbContext.KeyDtos.Find(Id);
        return found ?? new SshKeyDto
        {
            Id = Id,
            AbsolutePath = AbsoluteFilePath,
            Format = Format,
            Password = Password
        };
    }

    /// <summary>
    ///     Deletes the key file associated with the specified key.
    /// </summary>
    /// <remarks>
    ///     If the key format is OpenSSH and the file extension is ".pub", only the public key file will be deleted.
    ///     Otherwise, both the public and private key files will be deleted.
    /// </remarks>
    /// <param name="key">The key for which to delete the associated files.</param>
    public void DeleteKey()
    {
        switch (this)
        {
            case ISshPublicKey pub:
                FileOperations.Delete(AbsoluteFilePath);
                pub.PrivateKey.DeleteKey();
                break;
            default:
                FileOperations.Delete(AbsoluteFilePath);
                break;
        }
    }

    /// <summary>
    ///     Retrieves the private key source for the SSH key.
    /// </summary>
    /// <returns>The private key source for the SSH key.</returns>
    private IPrivateKeySource GetKeySource()
    {
        return Path.GetExtension(AbsoluteFilePath) switch
        {
            var x when x.Contains("ppk") => new PuttyKeyFile(AbsoluteFilePath, Password),
            var x when x.Contains("pub") => new PrivateKeyFile(Path.ChangeExtension(AbsoluteFilePath, null),
                Password),
            _ => new PrivateKeyFile(AbsoluteFilePath, Password)
        };
    }

    /// <summary>
    ///     Sets the private key source for the SSH key.
    /// </summary>
    /// <remarks>
    ///     This method is used to set the private key source for the SSH key. The private key source
    ///     contains the actual key material and is needed for cryptographic operations.
    /// </remarks>
    /// <param name="keySource">
    ///     The private key source to set for the SSH key.
    /// </param>
    private void SetKeySource()
    {
        try
        {
            _keySource = GetKeySource();
            if (Password is not null) PasswordSuccess = true;
        }
        catch (SshPassPhraseNullOrEmptyException e)
        {
            Password = "";
        }
        catch (Exception e)
        {
            // ignored
        }
    }
}