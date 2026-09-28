using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Interfaces;
using OpenSSH_GUI.Core.Lib.HostKeys;
using OpenSSH_GUI.SshConfig.Parsers;

namespace OpenSSH_GUI.Core.Services;

/// <summary>
///     <see cref="IKnownHostKeyStore" /> backed by the user's <c>known_hosts</c> file.
///     Supports plain and hashed (<c>|1|salt|hash</c>) host names, <c>[host]:port</c> entries,
///     wildcard and negated patterns as well as the <c>@revoked</c> marker.
///     <c>@cert-authority</c> entries are ignored.
/// </summary>
public sealed class KnownHostKeyStore : IKnownHostKeyStore
{
    private const string HashedHostPrefix = "|1|";
    private const string RevokedMarker = "@revoked";

    private readonly Lock _fileLock = new();
    private readonly string _knownHostsPath;
    private readonly ILogger<KnownHostKeyStore> _logger;

    public KnownHostKeyStore(ILogger<KnownHostKeyStore> logger)
        : this(logger, SshConfigFiles.Known_Hosts.GetPathOfFile()) { }

    public KnownHostKeyStore(ILogger<KnownHostKeyStore> logger, string knownHostsPath)
    {
        _logger = logger;
        _knownHostsPath = knownHostsPath;
    }

    /// <inheritdoc />
    public HostKeyVerificationStatus Verify(HostKeyInfo hostKey)
    {
        string[] lines;
        lock (_fileLock)
        {
            if (!File.Exists(_knownHostsPath)) return HostKeyVerificationStatus.Unknown;
            lines = File.ReadAllLines(_knownHostsPath);
        }

        var hostName = hostKey.KnownHostsName;
        var trusted = false;
        var mismatch = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line[0] == '#') continue;

            var fields = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            string? marker = null;
            if (fields[0][0] == '@')
            {
                marker = fields[0];
                fields = fields[1..];
            }

            if (fields.Length < 3) continue;
            if (!HostPatternMatches(hostName, fields[0])) continue;

            byte[] storedKey;
            try
            {
                storedKey = Convert.FromBase64String(fields[2]);
            }
            catch (FormatException)
            {
                continue;
            }

            var sameKey = CryptographicOperations.FixedTimeEquals(storedKey, hostKey.KeyBlob);
            switch (marker)
            {
                case RevokedMarker when sameKey:
                    return HostKeyVerificationStatus.Revoked;
                case null when sameKey:
                    trusted = true;
                    break;
                case null when string.Equals(HostKeyInfo.ReadKeyType(storedKey), hostKey.KeyType, StringComparison.Ordinal):
                    mismatch = true;
                    break;
            }
        }

        if (trusted) return HostKeyVerificationStatus.Trusted;
        if (mismatch)
        {
            _logger.LogWarning(
                "Host key mismatch for {host}: {keyType} {fingerprint}", hostName, hostKey.KeyType,
                hostKey.FingerprintSha256);
            return HostKeyVerificationStatus.Mismatch;
        }

        return HostKeyVerificationStatus.Unknown;
    }

    /// <inheritdoc />
    public void Add(HostKeyInfo hostKey)
    {
        var entry = $"{hostKey.KnownHostsName} {hostKey.KeyType} {Convert.ToBase64String(hostKey.KeyBlob)}\n";
        lock (_fileLock)
        {
            var directory = Path.GetDirectoryName(_knownHostsPath);
            if (!string.IsNullOrEmpty(directory))
            {
                if (OperatingSystem.IsWindows())
                    Directory.CreateDirectory(directory);
                else
                    Directory.CreateDirectory(
                        directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }

            var options = new FileStreamOptions
            {
                Access = FileAccess.ReadWrite,
                Mode = FileMode.OpenOrCreate,
                Share = FileShare.Read
            };
            if (!OperatingSystem.IsWindows())
                options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;

            using var stream = new FileStream(_knownHostsPath, options);
            if (stream.Length > 0)
            {
                stream.Seek(-1, SeekOrigin.End);
                if (stream.ReadByte() != '\n') entry = "\n" + entry;
            }

            stream.Seek(0, SeekOrigin.End);
            stream.Write(Encoding.ASCII.GetBytes(entry));
        }

        _logger.LogInformation(
            "Added {keyType} host key {fingerprint} for {host} to {path}", hostKey.KeyType,
            hostKey.FingerprintSha256, hostKey.KnownHostsName, _knownHostsPath);
    }

    private static bool HostPatternMatches(string hostName, string patterns)
    {
        if (patterns.StartsWith(HashedHostPrefix, StringComparison.Ordinal))
            return HashedHostMatches(hostName, patterns);

        return SshWildcardMatcher.Matches(hostName, patterns.Split(','));
    }

    private static bool HashedHostMatches(string hostName, string hashedEntry)
    {
        // |1|<base64 salt>|<base64 HMAC-SHA1(salt, host)>
        var parts = hashedEntry.Split('|');
        if (parts.Length != 4) return false;
        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            var actual = HMACSHA1.HashData(salt, Encoding.UTF8.GetBytes(hostName));
            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
