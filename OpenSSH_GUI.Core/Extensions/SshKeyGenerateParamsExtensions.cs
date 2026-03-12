using System.Text;
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
    /// <param name="generateParams">The <see cref="SshKeyGenerateParams" /> object to convert.</param>
    /// <returns>An <see cref="SshKeyGenerateInfo" /> object.</returns>
    public static SshKeyGenerateInfo ToInfo(this SshKeyGenerateParams generateParams)
    {
        var password = generateParams.Password is { } paramsPassword
            ? Encoding.UTF8.GetString(paramsPassword.Span)
            : null;
        return new SshKeyGenerateInfo
        {
            KeyType = generateParams.KeyType,
            Comment = generateParams.Comment,
            KeyFormat = generateParams.KeyFormat,
            KeyLength = generateParams.KeyLength,
            Encryption = !string.IsNullOrWhiteSpace(password)
                ? new SshKeyEncryptionAes256(password,
                    generateParams.KeyFormat is SshKeyFormat.PuTTYv3 ? new PuttyV3Encryption() : null)
                : new SshKeyEncryptionNone()
        };
    }
}