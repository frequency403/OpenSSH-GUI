using OpenSSH_GUI.Core.Lib.HostKeys;

namespace OpenSSH_GUI.Core.Interfaces;

/// <summary>
///     Verifies server host keys against the user's <c>known_hosts</c> file and records newly trusted keys.
/// </summary>
public interface IKnownHostKeyStore
{
    /// <summary>
    ///     Checks the presented host key against the <c>known_hosts</c> entries of the host.
    /// </summary>
    HostKeyVerificationStatus Verify(HostKeyInfo hostKey);

    /// <summary>
    ///     Appends the host key to <c>known_hosts</c>.
    /// </summary>
    void Add(HostKeyInfo hostKey);
}
