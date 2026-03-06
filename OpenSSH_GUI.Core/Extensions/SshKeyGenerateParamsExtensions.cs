using OpenSSH_GUI.Core.Lib.Misc;
using SshNet.Keygen;
using SshNet.Keygen.SshKeyEncryption;

namespace OpenSSH_GUI.Core.Extensions;

/// <summary>
///     Extensions for the <see cref="SshKeyGenerateParams" /> class.
/// </summary>
public static class SshKeyGenerateParamsExtensions
{
    /// <summary>
    ///     Converts the given <see cref="SshKeyGenerateParams" /> object to an <see cref="SshKeyGenerateInfo" /> object.
    /// </summary>
    /// <param name="params">The <see cref="SshKeyGenerateParams" /> object to convert.</param>
    /// <returns>An <see cref="SshKeyGenerateInfo" /> object.</returns>
    public static SshKeyGenerateInfo ToInfo(this SshKeyGenerateParams @params)
    {
        return new SshKeyGenerateInfo
        {
            KeyType = @params.KeyType,
            Comment = @params.Comment,
            KeyFormat = @params.KeyFormat,
            KeyLength = @params.KeyLength,
            Encryption = !string.IsNullOrWhiteSpace(@params.Password)
                ? new SshKeyEncryptionAes256(@params.Password,
                    @params.KeyFormat is not SshKeyFormat.OpenSSH ? new PuttyV3Encryption() : null)
                : new SshKeyEncryptionNone()
        };
    }
}