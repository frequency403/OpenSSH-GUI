using OpenSSH_GUI.Core.Lib.HostKeys;

namespace OpenSSH_GUI.Core.Interfaces;

/// <summary>
///     Asks the user whether an unknown server host key should be trusted (trust on first use).
/// </summary>
public interface IHostKeyTrustPrompt
{
    /// <returns><c>true</c> if the user confirmed the fingerprint and the key should be trusted.</returns>
    Task<bool> ConfirmUnknownHostKeyAsync(HostKeyInfo hostKey);
}
