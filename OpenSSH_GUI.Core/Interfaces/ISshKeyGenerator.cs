using OpenSSH_GUI.Core.Lib.Keys;
using SshNet.Keygen;

namespace OpenSSH_GUI.Core.Interfaces;

public interface ISshKeyGenerator
{
    /// <summary>
    ///     Generates a new SSH key.
    /// </summary>
    /// <param name="fullFilePath">The full path where the new key should be stored.</param>
    /// <param name="generateParamsInfo">Parameters for key generation.</param>
    /// <param name="overwrite">Whether to overwrite existing file if it exists.</param>
    /// <returns>A value task representing the asynchronous operation.</returns>
    ValueTask<SshKeyFile> Generate(string fullFilePath, SshKeyGenerateInfo generateParamsInfo, bool overwrite = false);
}