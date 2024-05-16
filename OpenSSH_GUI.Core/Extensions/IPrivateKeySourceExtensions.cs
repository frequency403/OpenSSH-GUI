// File Created by: Oliver Schantz
// Created: 16.05.2024 - 08:05:28
// Last edit: 16.05.2024 - 08:05:28

using Renci.SshNet;
using SshNet.Keygen.Extensions;
using SshNet.Keygen.SshKeyEncryption;

namespace OpenSSH_GUI.Core.Extensions;

public static class PrivateKeySourceExtensions
{
    public static string FingerprintHash(this IPrivateKeySource privateKeySource) => 
        privateKeySource
        .Fingerprint()
        .Split(' ')
        .First(e => e.StartsWith("SHA"))
        .Split(':')
        .Last();
}