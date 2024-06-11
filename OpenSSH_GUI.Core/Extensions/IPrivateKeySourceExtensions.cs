// File Created by: Oliver Schantz
// Created: 16.05.2024 - 08:05:28
// Last edit: 16.05.2024 - 08:05:28

using Renci.SshNet;
using SshNet.Keygen.Extensions;

namespace OpenSSH_GUI.Core.Extensions;

/// <summary>
///     Provides extension methods for the IPrivateKeySource interface.
/// </summary>
public static class PrivateKeySourceExtensions
{
    /// <summary>
    ///     Retrieves the fingerprint hash of the private key source.
    /// </summary>
    /// <param name="privateKeySource">The private key source.</param>
    /// <returns>The fingerprint hash of the private key source.</returns>
    public static string FingerprintHash(this IPrivateKeySource privateKeySource)
    {
        return privateKeySource
            .Fingerprint()
            .Split(' ')
            .First(e => e.StartsWith("SHA"))
            .Split(':')
            .Last();
    }
}