namespace OpenSSH_GUI.Core.Lib.HostKeys;

/// <summary>
///     Thrown when a connection is aborted because the server host key could not be verified.
/// </summary>
public sealed class HostKeyVerificationException(HostKeyInfo hostKey, HostKeyVerificationStatus status)
    : Exception(CreateMessage(hostKey, status))
{
    public HostKeyInfo HostKey { get; } = hostKey;

    public HostKeyVerificationStatus Status { get; } = status;

    private static string CreateMessage(HostKeyInfo hostKey, HostKeyVerificationStatus status) => status switch
    {
        HostKeyVerificationStatus.Mismatch =>
            $"The {hostKey.KeyType} host key of {hostKey.KnownHostsName} does not match the key in known_hosts " +
            $"({hostKey.FingerprintSha256}). Someone could be eavesdropping on the connection (man-in-the-middle " +
            "attack), or the host key has been changed. Connection aborted.",
        HostKeyVerificationStatus.Revoked =>
            $"The {hostKey.KeyType} host key of {hostKey.KnownHostsName} ({hostKey.FingerprintSha256}) " +
            "is marked as revoked in known_hosts. Connection aborted.",
        _ =>
            $"The authenticity of host {hostKey.KnownHostsName} ({hostKey.KeyType} {hostKey.FingerprintSha256}) " +
            "was not confirmed. Connection aborted."
    };
}
