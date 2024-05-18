// File Created by: Oliver Schantz
// Created: 18.05.2024 - 13:05:14
// Last edit: 18.05.2024 - 13:05:14

using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Lib.Misc;
using SshNet.Keygen;
using SshNet.Keygen.SshKeyEncryption;

namespace OpenSSH_GUI.Core.Extensions;

public static class SshKeyGenerateParamsExtensions
{
    public static SshKeyGenerateInfo ToInfo(this SshKeyGenerateParams @params) =>
        new()
        {
            KeyType = @params.KeyType switch
            {
                KeyType.RSA => SshKeyType.RSA,
                KeyType.ECDSA => SshKeyType.ECDSA,
                KeyType.ED25519 => SshKeyType.ED25519,
                _ => throw new ArgumentOutOfRangeException()
            },
            Comment = @params.Comment,
            KeyFormat = @params.KeyFormat,
            KeyLength = @params.KeyLength,
            Encryption = !string.IsNullOrWhiteSpace(@params.Password)
                ? new SshKeyEncryptionAes256(@params.Password, @params.KeyFormat is not SshKeyFormat.OpenSSH ? new PuttyV3Encryption() : null)
                : new SshKeyEncryptionNone()

        };
}