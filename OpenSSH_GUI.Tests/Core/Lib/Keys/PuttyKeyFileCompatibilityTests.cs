using Renci.SshNet;
using Shouldly;
using SshNet.Keygen;
using SshNet.Keygen.Extensions;
using SshNet.Keygen.SshKeyEncryption;
using Xunit;

namespace OpenSSH_GUI.Tests.Core.Lib.Keys;

/// <summary>
///     Guards the PPK round trip across SSH.NET and SshNet.Keygen upgrades: keys written by SshNet.Keygen in PuTTY format
///     must load with SSH.NET's <see cref="PrivateKeyFile" />, which is how the app opens <c>.ppk</c> files.
/// </summary>
public sealed class PuttyKeyFileCompatibilityTests : IDisposable
{
    private readonly string directory = Directory.CreateTempSubdirectory("putty-compat").FullName;

    public void Dispose() { Directory.Delete(directory, true); }

    [Theory]
    [InlineData(SshKeyType.RSA, SshKeyFormat.PuTTYv2, null)]
    [InlineData(SshKeyType.RSA, SshKeyFormat.PuTTYv3, null)]
    [InlineData(SshKeyType.ED25519, SshKeyFormat.PuTTYv2, null)]
    [InlineData(SshKeyType.ED25519, SshKeyFormat.PuTTYv3, null)]
    [InlineData(SshKeyType.ECDSA, SshKeyFormat.PuTTYv3, null)]
    [InlineData(SshKeyType.RSA, SshKeyFormat.PuTTYv2, "ppk-passphrase")]
    [InlineData(SshKeyType.ED25519, SshKeyFormat.PuTTYv3, "ppk-passphrase")]
    [InlineData(SshKeyType.ECDSA, SshKeyFormat.PuTTYv3, "ppk-passphrase")]
    public void PrivateKeyFile_PpkWrittenBySshNetKeygen_LoadsSameKey(SshKeyType keyType, SshKeyFormat format, string? passphrase)
    {
        var path = Path.Combine(directory, $"id_{keyType}_{format}.ppk");
        var info = new SshKeyGenerateInfo(keyType)
        {
            Comment = "putty-compat",
            KeyFormat = format
        };
        if (passphrase is not null) info.Encryption = new SshKeyEncryptionAes256(passphrase);
        var generated = SshKey.Generate(path, FileMode.CreateNew, info);

        using var loaded = passphrase is null ? new PrivateKeyFile(path) : new PrivateKeyFile(path, passphrase);

        GetPublicKeyBlob(loaded).ShouldBe(GetPublicKeyBlob(generated));
    }

    private static string GetPublicKeyBlob(IPrivateKeySource keySource) => keySource.ToOpenSshPublicFormat().Split(' ')[1];
}
