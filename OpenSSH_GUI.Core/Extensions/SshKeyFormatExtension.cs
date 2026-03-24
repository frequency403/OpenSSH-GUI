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
    public const string OpenSshPublicKeyFileExtension = ".pub";

    /// <summary>
    ///     Represents the file extension used for PuTTY private key files.
    /// </summary>
    public const string PuttyKeyFileExtension = ".ppk";

    /// <param name="format">The SSH key format.</param>
    extension(SshKeyFormat format)
    {
        /// <summary>
        ///     Returns the file extension associated with the specified SSH key format.
        /// </summary>
        /// <param name="usePublicFormat">True if the extension is for a public key; otherwise, false. Defaults to true.</param>
        /// <returns>
        ///     The file extension associated with the specified SSH key format. Returns null if the format is not supported
        ///     or the extension is not applicable.
        /// </returns>
        public string? GetExtension(bool usePublicFormat = true)
        {
            return format switch
            {
                SshKeyFormat.OpenSSH when usePublicFormat => OpenSshPublicKeyFileExtension,
                SshKeyFormat.OpenSSH => null,
                SshKeyFormat.PuTTYv2 or SshKeyFormat.PuTTYv3 => PuttyKeyFileExtension,
                _ => null
            };
        }

        /// <summary>
        ///     Changes the file extension of the given path to match the specified SSH key format.
        /// </summary>
        /// <param name="path">The path to the file.</param>
        /// <param name="usePublicFormat">Indicates whether the key is public. Default is true.</param>
        /// <returns>The modified file path with the updated extension.</returns>
        public string ChangeExtension(string path, bool usePublicFormat = true)
        {
            return Path.ChangeExtension(path, format.GetExtension(usePublicFormat));
        }
    }
}