using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using OpenSSH_GUI.Core.Lib.HostKeys;
using OpenSSH_GUI.Core.Services;
using Shouldly;
using Xunit;

namespace OpenSSH_GUI.Tests.Core.Services;

public sealed class KnownHostKeyStoreTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("knownhosts").FullName;
    private readonly string _path;
    private readonly KnownHostKeyStore _store;

    public KnownHostKeyStoreTests()
    {
        _path = Path.Combine(_directory, "known_hosts");
        _store = new KnownHostKeyStore(NullLogger<KnownHostKeyStore>.Instance, _path);
    }

    public void Dispose() { Directory.Delete(_directory, true); }

    private static byte[] CreateKeyBlob(string keyType, byte fill)
    {
        var type = Encoding.ASCII.GetBytes(keyType);
        var blob = new byte[4 + type.Length + 32];
        blob[3] = (byte)type.Length;
        type.CopyTo(blob, 4);
        blob.AsSpan(4 + type.Length).Fill(fill);
        return blob;
    }

    private static readonly byte[] Ed25519Key = CreateKeyBlob("ssh-ed25519", 1);
    private static readonly byte[] OtherEd25519Key = CreateKeyBlob("ssh-ed25519", 2);
    private static readonly byte[] EcdsaKey = CreateKeyBlob("ecdsa-sha2-nistp256", 3);

    private void WriteKnownHosts(params string[] lines) => File.WriteAllLines(_path, lines);

    private static string Entry(string hosts, byte[] key, string? marker = null) =>
        $"{(marker is null ? string.Empty : marker + " ")}{hosts} {HostKeyInfo.ReadKeyType(key)} {Convert.ToBase64String(key)}";

    [Fact]
    public void Verify_MissingFile_ReturnsUnknown()
    {
        _store.Verify(new HostKeyInfo("example.com", 22, Ed25519Key)).ShouldBe(HostKeyVerificationStatus.Unknown);
    }

    [Theory]
    [InlineData("example.com", 22, "example.com")]
    [InlineData("EXAMPLE.com", 22, "other.org,example.com")]
    [InlineData("example.com", 2222, "[example.com]:2222")]
    [InlineData("host1.example.com", 22, "*.example.com")]
    public void Verify_MatchingEntry_ReturnsTrusted(string host, int port, string pattern)
    {
        WriteKnownHosts("# comment", string.Empty, Entry(pattern, Ed25519Key));

        _store.Verify(new HostKeyInfo(host, port, Ed25519Key)).ShouldBe(HostKeyVerificationStatus.Trusted);
    }

    [Fact]
    public void Verify_HashedEntry_ReturnsTrusted()
    {
        var salt = RandomNumberGenerator.GetBytes(20);
        var hash = HMACSHA1.HashData(salt, Encoding.UTF8.GetBytes("example.com"));
        WriteKnownHosts(Entry($"|1|{Convert.ToBase64String(salt)}|{Convert.ToBase64String(hash)}", Ed25519Key));

        _store.Verify(new HostKeyInfo("example.com", 22, Ed25519Key)).ShouldBe(HostKeyVerificationStatus.Trusted);
        _store.Verify(new HostKeyInfo("example.org", 22, Ed25519Key)).ShouldBe(HostKeyVerificationStatus.Unknown);
    }

    [Fact]
    public void Verify_DefaultPortEntry_DoesNotMatchOtherPort()
    {
        WriteKnownHosts(Entry("example.com", Ed25519Key));

        _store.Verify(new HostKeyInfo("example.com", 2222, Ed25519Key)).ShouldBe(HostKeyVerificationStatus.Unknown);
    }

    [Fact]
    public void Verify_NegatedPattern_ReturnsUnknown()
    {
        WriteKnownHosts(Entry("*.example.com,!evil.example.com", Ed25519Key));

        _store.Verify(new HostKeyInfo("evil.example.com", 22, Ed25519Key))
            .ShouldBe(HostKeyVerificationStatus.Unknown);
    }

    [Fact]
    public void Verify_SameTypeDifferentKey_ReturnsMismatch()
    {
        WriteKnownHosts(Entry("example.com", Ed25519Key));

        _store.Verify(new HostKeyInfo("example.com", 22, OtherEd25519Key))
            .ShouldBe(HostKeyVerificationStatus.Mismatch);
    }

    [Fact]
    public void Verify_OnlyOtherKeyTypeKnown_ReturnsUnknown()
    {
        WriteKnownHosts(Entry("example.com", EcdsaKey));

        _store.Verify(new HostKeyInfo("example.com", 22, Ed25519Key)).ShouldBe(HostKeyVerificationStatus.Unknown);
    }

    [Fact]
    public void Verify_RevokedKey_ReturnsRevokedEvenIfTrusted()
    {
        WriteKnownHosts(Entry("example.com", Ed25519Key), Entry("*", Ed25519Key, "@revoked"));

        _store.Verify(new HostKeyInfo("example.com", 22, Ed25519Key)).ShouldBe(HostKeyVerificationStatus.Revoked);
    }

    [Fact]
    public void Verify_CertAuthorityEntry_IsIgnored()
    {
        WriteKnownHosts(Entry("example.com", Ed25519Key, "@cert-authority"));

        _store.Verify(new HostKeyInfo("example.com", 22, Ed25519Key)).ShouldBe(HostKeyVerificationStatus.Unknown);
    }

    [Fact]
    public void Add_AppendsEntryThatVerifiesAsTrusted()
    {
        File.WriteAllText(_path, Entry("other.org", EcdsaKey)); // no trailing newline
        var hostKey = new HostKeyInfo("Example.com", 2222, Ed25519Key);

        _store.Add(hostKey);

        _store.Verify(hostKey).ShouldBe(HostKeyVerificationStatus.Trusted);
        File.ReadAllLines(_path).ShouldBe([Entry("other.org", EcdsaKey), Entry("[example.com]:2222", Ed25519Key)]);
    }

    [Fact]
    public void Add_NewFile_IsCreatedOwnerOnly()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Unix file modes only");

        _store.Add(new HostKeyInfo("example.com", 22, Ed25519Key));

#pragma warning disable CA1416
        File.GetUnixFileMode(_path).ShouldBe(UnixFileMode.UserRead | UnixFileMode.UserWrite);
#pragma warning restore CA1416
    }

    [Fact]
    public void HostKeyInfo_FingerprintMatchesOpenSshNotation()
    {
        var expected = "SHA256:" + Convert.ToBase64String(SHA256.HashData(Ed25519Key)).TrimEnd('=');

        new HostKeyInfo("example.com", 22, Ed25519Key).FingerprintSha256.ShouldBe(expected);
    }
}
