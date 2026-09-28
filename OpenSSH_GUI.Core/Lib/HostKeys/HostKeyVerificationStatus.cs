namespace OpenSSH_GUI.Core.Lib.HostKeys;

/// <summary>
///     Result of checking a server host key against the local <c>known_hosts</c> file.
/// </summary>
public enum HostKeyVerificationStatus
{
    /// <summary>No entry exists for the host and key type.</summary>
    Unknown,

    /// <summary>An entry for the host matches the presented key.</summary>
    Trusted,

    /// <summary>The host is known with a different key of the same type - possible man-in-the-middle attack.</summary>
    Mismatch,

    /// <summary>The presented key is marked as <c>@revoked</c>.</summary>
    Revoked
}
