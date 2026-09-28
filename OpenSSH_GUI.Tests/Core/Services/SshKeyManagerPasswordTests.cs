using System.Text;
using Avalonia.Headless.XUnit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using OpenSSH_GUI.Core.Interfaces;
using OpenSSH_GUI.Core.Lib.Keys;
using OpenSSH_GUI.Core.Lib.Misc;
using OpenSSH_GUI.Core.Services;
using Renci.SshNet;
using Renci.SshNet.Common;
using Shouldly;
using SshNet.Keygen;
using SshNet.Keygen.Extensions;
using SshNet.Keygen.SshKeyEncryption;
using Xunit;

namespace OpenSSH_GUI.Tests.Core.Services;

public sealed class SshKeyManagerPasswordTests : IDisposable
{
    private readonly string _directory = Directory.CreateTempSubdirectory("keymanager").FullName;

    public void Dispose() { Directory.Delete(_directory, true); }

    private static SshKeyManager CreateManager()
    {
        var backupService = Substitute.For<IKeyFileBackupService>();
        backupService.BackupFiles(Arg.Any<FileInfo[]>()).Returns([]);
        return new SshKeyManager(
            NullLogger<SshKeyManager>.Instance,
            Substitute.For<IDirectoryCrawler>(),
            Substitute.For<ISshKeyFactory>(),
            Substitute.For<ISshKeyGenerator>(),
            new KeyFileWriterService(NullLogger<KeyFileWriterService>.Instance),
            backupService);
    }

    private SshKeyFile LoadKey(string? passphrase, out string path)
    {
        path = Path.Combine(_directory, "id_test");
        var info = new SshKeyGenerateInfo(SshKeyType.ED25519) { Comment = "test" };
        if (passphrase is not null) info.Encryption = new SshKeyEncryptionAes256(passphrase);
        var generated = SshKey.Generate(path, FileMode.Create, info);
        File.WriteAllText(path + ".pub", generated.ToOpenSshPublicFormat());

        var key = new SshKeyFile(Substitute.For<ILogger<SshKeyFile>>());
        if (passphrase is null)
            key.Load(SshKeyFileSource.FromDisk(path));
        else
            key.Load(SshKeyFileSource.FromDisk(path), Encoding.UTF8.GetBytes(passphrase));
        return key;
    }

    [AvaloniaTheory]
    [InlineData("new-passphrase")]
    [InlineData("with \"quotes\" -N and spaces")]
    public async Task ChangePasswordOfKeyAsync_EncryptsKeyWithNewPassphrase(string newPassphrase)
    {
        using var manager = CreateManager();
        var key = LoadKey(null, out var path);
        key.PrivateKeyFile.ShouldNotBeNull();

        var result = await manager.ChangePasswordOfKeyAsync(key, Encoding.UTF8.GetBytes(newPassphrase));

        result.IsSuccess.ShouldBeTrue();
        Should.Throw<SshPassPhraseNullOrEmptyException>(() => new PrivateKeyFile(path));
        Should.NotThrow(() => new PrivateKeyFile(path, newPassphrase));
    }

    [AvaloniaFact]
    public async Task ChangePasswordOfKeyAsync_EmptyPassword_RemovesEncryption()
    {
        using var manager = CreateManager();
        var key = LoadKey("old-passphrase", out var path);
        key.PrivateKeyFile.ShouldNotBeNull();

        var result = await manager.ChangePasswordOfKeyAsync(key, ReadOnlyMemory<byte>.Empty);

        result.IsSuccess.ShouldBeTrue();

        Should.NotThrow(() => new PrivateKeyFile(path));
    }
}
