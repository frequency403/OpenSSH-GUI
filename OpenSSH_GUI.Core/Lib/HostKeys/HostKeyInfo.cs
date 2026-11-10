using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace OpenSSH_GUI.Core.Lib.HostKeys;

/// <summary>
///     A server host key as presented during the SSH key exchange.
/// </summary>
/// <param name="Host">The host name the connection was made to.</param>
/// <param name="Port">The port the connection was made to.</param>
/// <param name="KeyBlob">The public key in SSH wire format.</param>
public sealed record HostKeyInfo(string Host, int Port, byte[] KeyBlob)
{
    /// <summary>
    ///     The key type encoded in the key blob, e.g. <c>ssh-ed25519</c>.
    /// </summary>
    public string KeyType { get; } = ReadKeyType(KeyBlob) ?? string.Empty;

    /// <summary>
    ///     The fingerprint in OpenSSH notation, e.g. <c>SHA256:...</c>.
    /// </summary>
    public string FingerprintSha256 =>
        $"SHA256:{Convert.ToBase64String(SHA256.HashData(KeyBlob)).TrimEnd('=')}";

    /// <summary>
    ///     The host name as written to and matched against <c>known_hosts</c>
    ///     (<c>[host]:port</c> for non-default ports).
    /// </summary>
    public string KnownHostsName => GetKnownHostsName(Host, Port);

    public static string GetKnownHostsName(string host, int port)
    {
        // OpenSSH canonicalizes host names to lower case before looking them up in known_hosts.
        var name = host.ToLowerInvariant();
        return port == 22 ? name : $"[{name}]:{port}";
    }

    /// <summary>
    ///     Reads the leading key type string of an SSH wire format public key.
    /// </summary>
    public static string? ReadKeyType(ReadOnlySpan<byte> keyBlob)
    {
        if (keyBlob.Length < sizeof(uint)) return null;
        var length = BinaryPrimitives.ReadUInt32BigEndian(keyBlob);
        if (length == 0 || length > keyBlob.Length - sizeof(uint)) return null;
        return Encoding.ASCII.GetString(keyBlob.Slice(sizeof(uint), (int)length));
    }
}
