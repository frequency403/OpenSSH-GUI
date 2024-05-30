// File Created by: Oliver Schantz
// Created: 18.05.2024 - 14:05:52
// Last edit: 18.05.2024 - 14:05:52

using SshNet.Keygen;

namespace OpenSSH_GUI.Core.Extensions;

/// <summary>
///     Provides extension methods for working with SSH key formats.
/// </summary>
public static class SshKeyFormatExtension
{
    /// <summary>
    ///     Represents the file extension for OpenSSH Public Key format.
    /// </summary>
    private const string OpenSshPublicKeyExtension = ".pub";

    /// <summary>
    ///     Represents the file extension used for PuTTY private key files.
    /// </summary>
    private const string PuttyKeyExtension = ".ppk";

    /// <summary>
    ///     Returns the file extension associated with the specified SSH key format.
    /// </summary>
    /// <param name="format">The SSH key format.</param>
    /// <param name="public">True if the extension is for a public key; otherwise, false. Defaults to true.</param>
    /// <returns>
    ///     The file extension associated with the specified SSH key format. Returns null if the format is not supported
    ///     or the extension is not applicable.
    /// </returns>
    public static string? GetExtension(this SshKeyFormat format, bool @public = true)
    {
        return format switch
        {
            SshKeyFormat.OpenSSH when @public => OpenSshPublicKeyExtension,
            SshKeyFormat.OpenSSH => null,
            SshKeyFormat.PuTTYv2 or SshKeyFormat.PuTTYv3 => PuttyKeyExtension,
            _ => null
        };
    }

    /// <summary>
    ///     Changes the file extension of the given path to match the specified SSH key format.
    /// </summary>
    /// <param name="format">The SSH key format.</param>
    /// <param name="path">The path to the file.</param>
    /// <param name="public">Indicates whether the key is public. Default is true.</param>
    /// <returns>The modified file path with the updated extension.</returns>
    public static string ChangeExtension(this SshKeyFormat format, string path, bool @public = true)
    {
        return Path.ChangeExtension(path, format.GetExtension(@public));
    }
}