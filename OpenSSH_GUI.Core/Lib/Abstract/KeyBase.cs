#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:46
// Last edit: 15.05.2024 - 01:05:48

#endregion

using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Interfaces.AuthorizedKeys;
using OpenSSH_GUI.Core.Interfaces.Keys;
using OpenSSH_GUI.Core.Interfaces.Misc;
using OpenSSH_GUI.Core.Lib.AuthorizedKeys;
using OpenSSH_GUI.Core.Lib.Keys;
using Renci.SshNet;
using Renci.SshNet.Common;
using SshNet.Keygen;
using SshNet.Keygen.Extensions;
using SshNet.PuttyKeyFile;

namespace OpenSSH_GUI.Core.Lib.Abstract;

/// <summary>
/// Base class for SSH keys.
/// </summary>
public abstract class KeyBase : IKeyBase
{
    private IPrivateKeySource? _keySource;

    /// <summary>
    /// Base class for SSH keys.
    /// </summary>
    protected KeyBase(string absoluteFilePath, string? password = null)
    {
        AbsoluteFilePath = absoluteFilePath;
        Filename = Path.GetFileNameWithoutExtension(AbsoluteFilePath);
        Password = password;
        try
        {
            _keySource = Path.GetExtension(AbsoluteFilePath) switch
            {
                var x when x.Contains("ppk") => new PuttyKeyFile(AbsoluteFilePath, Password),
                var x when x.Contains("pub") => new PrivateKeyFile(Path.ChangeExtension(AbsoluteFilePath, null),
                    Password),
                _ => new PrivateKeyFile(AbsoluteFilePath, Password)
            };
            if (password is not null) PasswordSuccess = true;
        }
        catch (SshPassPhraseNullOrEmptyException e)
        {
            Password = "";
            return;
        }
        catch (Exception e)
        {
            return;
        }
        var name = _keySource.HostKeyAlgorithms.FirstOrDefault()?.Name;
        Fingerprint = _keySource.FingerprintHash();
    }

    /// <summary>
    /// Gets a value indicating whether the key requires a password.
    /// </summary>
    /// <value>
    /// <c>true</c> if the key requires a password; otherwise, <c>false</c>.
    /// </value>
    public bool NeedPassword => HasPassword && !PasswordSuccess;

    /// <summary>
    /// Represents the success status of a password for accessing the private key file.
    /// </summary>
    private bool PasswordSuccess { get; }

    /// <summary>
    /// Represents a key that can be used for SSH authentication.
    /// </summary>
    public bool HasPassword => Password is not null;

    /// <summary>
    /// Represents a password for a SSH key.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Gets the filename of the key file.
    /// </summary>
    public string Filename { get; }

    /// <summary>
    /// Gets the absolute file path of the key.
    /// </summary>
    /// <value>
    /// The absolute file path of the key.
    /// </value>
    public string AbsoluteFilePath { get; private set; }

    /// <summary>
    /// Gets the format of the key.
    /// </summary>
    public SshKeyFormat Format { get; protected init; }

    /// <summary>
    /// Represents a fingerprint of a key.
    /// </summary>
    public string Fingerprint { get; protected set; }

    /// <summary>
    /// Exports the authorized key entry for the key.
    /// </summary>
    /// <returns>The authorized key entry for the key.</returns>
    public string? ExportAuthorizedKeyEntry() => ExportOpenSshPublicKey();

    /// <summary>
    /// Export the public key in OpenSSH format.
    /// </summary>
    /// <returns>The public key in OpenSSH format.</returns>
    public string? ExportOpenSshPublicKey() => _keySource?.ToOpenSshPublicFormat();

    /// <summary>
    /// Exports the private key in OpenSSH format.
    /// </summary>
    /// <returns>The private key in OpenSSH format as a string, or null if the key source is null.</returns>
    public string? ExportOpenSshPrivateKey() => _keySource?.ToOpenSshFormat();

    /// <summary>
    /// Exports the public key in PuTTY format.
    /// </summary>
    /// <returns>The public key in PuTTY format.</returns>
    public string? ExportPuttyPublicKey() => _keySource?.ToPuttyPublicFormat();

    /// <summary>
    /// Exports the Putty PPK key format of the SSH key.
    /// </summary>
    /// <returns>The Putty PPK key format as a string.</returns>
    public string? ExportPuttyPpkKey() => _keySource?.ToPuttyFormat();

    /// <summary>
    /// Exports the text representation of the key.
    /// </summary>
    /// <returns>The text representation of the key.</returns>
    public abstract string ExportTextOfKey();

    /// <summary>
    /// Exports the authorized key as a string in OpenSSH format.
    /// </summary>
    /// <returns>The authorized key entry in OpenSSH format.</returns>
    public IAuthorizedKey ExportAuthorizedKey() => new AuthorizedKey(ExportAuthorizedKeyEntry());

    /// <summary>
    /// Gets the SSH.NET key type of the key.
    /// </summary>
    /// <returns>The SSH.NET key type of the key.</returns>
    public IPrivateKeySource? GetSshNetKeyType() => _keySource;

    /// <summary>
    /// Sets the password for the SSH key.
    /// </summary>
    /// <param name="password">The password to set.</param>
    /// <returns>The SSH key with the updated password.</returns>
    public ISshKey SetPassword(string password)
    {
        Password = password;
        return Path.GetExtension(AbsoluteFilePath) switch
        {
            var x when x.Contains("ppk") => new PpkKey(AbsoluteFilePath, password),
            var x when x.Contains("pub") => new SshPublicKey(AbsoluteFilePath, password),
            _ => new SshPrivateKey(Path.ChangeExtension(AbsoluteFilePath, null), password)
        };
    }

    /// <summary>
    /// Deletes the key file associated with the specified key.
    /// </summary>
    /// <remarks>
    /// If the key format is OpenSSH and the file extension is ".pub", only the public key file will be deleted.
    /// Otherwise, both the public and private key files will be deleted.
    /// </remarks>
    /// <param name="key">The key for which to delete the associated files.</param>
    public void DeleteKey()
    {
        if (Format is SshKeyFormat.OpenSSH && Path.GetExtension(AbsoluteFilePath).Contains("pub"))
        {
            File.Delete(Path.ChangeExtension(AbsoluteFilePath, null));
        }
        File.Delete(AbsoluteFilePath);
    }

    /// <summary>
    /// Asynchronously exports the key to disk in the specified format.
    /// </summary>
    /// <param name="format">The format in which to export the key.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// This method exports the key to disk in the specified format. The exported key file
    /// will be saved in the same directory as the original key file with the specified format
    /// as the file extension.
    /// </remarks>
    public Task ExportToDiskAsync(SshKeyFormat format) => new(() => ExportToDisk(format));

    /// <summary>
    /// Exports the SSH key to the disk in the specified format.
    /// </summary>
    /// <param name="format">The format in which to export the key (OpenSSH or PuTTYv2).</param>
    public void ExportToDisk(SshKeyFormat format) => ExportToDisk(format, out _);

    /// <summary>
    /// Exports the SSH key to disk in the specified format.
    /// </summary>
    /// <param name="format">The format to export the key in.</param>
    /// <param name="key">When the method returns, contains the exported SSH key. This parameter is passed uninitialized.</param>
    public void ExportToDisk(SshKeyFormat format, out ISshKey? key)
    {
        key = null;
        var privateFilePath = "";
        switch (format)
        {
            case SshKeyFormat.OpenSSH:
                privateFilePath = GetUniqueFilePath(Path.ChangeExtension(AbsoluteFilePath, null));
                var publicFilePath = Path.ChangeExtension(privateFilePath, ".pub");
                using (var privateWriter = new StreamWriter(privateFilePath, false))
                {
                    privateWriter.WriteAsync(ExportOpenSshPrivateKey());
                }

                using (var publicWriter = new StreamWriter(publicFilePath, false))
                {
                    publicWriter.WriteAsync(ExportOpenSshPublicKey());
                }

                key = new SshPublicKey(publicFilePath, Password);
                break;
            case SshKeyFormat.PuTTYv2:
            case SshKeyFormat.PuTTYv3:
            default:
                privateFilePath = GetUniqueFilePath(Path.ChangeExtension(AbsoluteFilePath, ".ppk"));
                using (var privateWriter = new StreamWriter(privateFilePath, false))
                {
                    privateWriter.WriteAsync(ExportPuttyPpkKey());
                }

                key = new PpkKey(privateFilePath, Password);
                break;
        }
    }

    /// <summary>
    /// Converts the SSH key to the specified format.
    /// </summary>
    /// <param name="format">The format to convert the key to.</param>
    /// <returns>The converted SSH key, or null if the key is already in the specified format.</returns>
    public ISshKey? Convert(SshKeyFormat format) => Convert(format, false, NullLogger.Instance);

    /// <summary>
    /// Converts the SSH key to the specified format.
    /// </summary>
    /// <param name="format">The format to convert the SSH key to.</param>
    /// <param name="logger">The logger to use for logging.</param>
    /// <returns>The converted SSH key.</returns>
    public ISshKey? Convert(SshKeyFormat format, ILogger logger) => Convert(format, false, logger);

    /// <summary>
    /// Converts the SSH key to the specified format.
    /// </summary>
    /// <param name="format">The format to convert the SSH key to.</param>
    /// <param name="move">Indicates whether to move the file after conversion.</param>
    /// <param name="logger">The logger to write any errors or messages.</param>
    /// <returns>
    /// The converted SSH key if successful, otherwise null.
    /// </returns>
    public ISshKey? Convert(SshKeyFormat format, bool move, ILogger logger)
    {
        if (format.Equals(Format)) return null;
        ExportToDisk(format, out var key);
        if (this is IPpkKey && move)
            try
            {
                var directory = Directory.GetParent(AbsoluteFilePath)!.CreateSubdirectory("PPK");
                var newFileDestination = Path.Combine(directory.FullName, Path.GetFileName(AbsoluteFilePath));
                File.Move(AbsoluteFilePath, newFileDestination);
                AbsoluteFilePath = newFileDestination;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error moving the file");
            }

        if (key is null) logger.LogError("Error converting the key");
        return key;
    }

    /// <summary>
    /// Generates a unique file path based on the original file path
    /// </summary>
    /// <param name="originalFilePath">The original file path</param>
    /// <returns>The unique file path</returns>
    private static string GetUniqueFilePath(string originalFilePath)
    {
        if (File.Exists(originalFilePath))
            originalFilePath = Path.Combine(
                Path.GetDirectoryName(originalFilePath),
                $"{Path.GetFileNameWithoutExtension(originalFilePath)}_{DateTime.Now:yy_MM_dd_HH_mm}");

        return originalFilePath;
    }
}