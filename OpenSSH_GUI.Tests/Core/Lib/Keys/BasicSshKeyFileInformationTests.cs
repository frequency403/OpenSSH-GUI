using Avalonia.Headless.XUnit;
using OpenSSH_GUI.Core.Lib.Keys;
using Shouldly;
using SshNet.Keygen;
using SshNet.Keygen.Extensions;
using SshNet.Keygen.SshKeyEncryption;
using Xunit;

namespace OpenSSH_GUI.Tests.Core.Lib.Keys;

public class BasicSshKeyFileInformationTests
{
    private static (string PrivatePath, string PublicPath) GenerateOpenSshKeyPair(string dir, SshKeyType type = SshKeyType.ED25519, string comment = "test-comment",
        string? passphrase = null, string name = "id_test", int? length = null)
    {
        var privatePath = Path.Combine(dir, name);
        var info = new SshKeyGenerateInfo(type) { Comment = comment };
        if (length is { } l) info.KeyLength = l;
        if (passphrase is not null) info.Encryption = new SshKeyEncryptionAes256(passphrase);
        var generated = SshKey.Generate(privatePath, FileMode.Create, info);
        var publicPath = privatePath + ".pub";
        File.WriteAllText(publicPath, generated.ToOpenSshPublicFormat());
        return (privatePath, publicPath);
    }

    private static string GeneratePpkKey(string dir, SshKeyType type = SshKeyType.RSA, SshKeyFormat format = SshKeyFormat.PuTTYv3, string comment = "ppk-comment",
        string name = "id_test.ppk")
    {
        var path = Path.Combine(dir, name);
        var info = new SshKeyGenerateInfo(type) { Comment = comment, KeyFormat = format };
        SshKey.Generate(path, FileMode.Create, info);
        return path;
    }

    [AvaloniaFact]
    public void FromKeyFileInfo_NonExistentFile_ReturnsEmpty()
    {
        var dir = Directory.CreateTempSubdirectory();
        try
        {
            var info = new SshKeyFileInformation(SshKeyFileSource.FromDisk(Path.Combine(dir.FullName, "does_not_exist")));
            var result = BasicSshKeyFileInformation.FromKeyFileInfo(info);

            result.FingerPrint.ShouldBeEmpty();
            result.Comment.ShouldBeEmpty();
            result.BitLength.ShouldBe(0);
            result.KeyType.ShouldBe(SshKeyType.RSA);
            result.Format.ShouldBe(SshKeyFormat.OpenSSH);
        }
        finally
        {
            dir.Delete(true);
        }
    }

    [AvaloniaFact]
    public void FromKeyFileInfo_OpenSshEd25519WithPubFile_ParsesCorrectly()
    {
        var dir = Directory.CreateTempSubdirectory();
        try
        {
            var (privatePath, _) = GenerateOpenSshKeyPair(dir.FullName, SshKeyType.ED25519, "alice@host");

            var info = new SshKeyFileInformation(SshKeyFileSource.FromDisk(privatePath));
            var result = BasicSshKeyFileInformation.FromKeyFileInfo(info);

            result.Comment.ShouldBe("alice@host");
            result.KeyType.ShouldBe(SshKeyType.ED25519);
            result.BitLength.ShouldBe(256);
            result.Format.ShouldBe(SshKeyFormat.OpenSSH);
            result.HashAlgorithmName.ShouldBe(SshKeyHashAlgorithmName.SHA256);
            result.FingerPrint.ShouldNotBeNullOrEmpty();
        }
        finally
        {
            dir.Delete(true);
        }
    }

    [AvaloniaFact]
    public void FromKeyFileInfo_OpenSshRsaWithPubFile_ParsesCorrectBitLength()
    {
        var dir = Directory.CreateTempSubdirectory();
        try
        {
            var (privatePath, _) = GenerateOpenSshKeyPair(dir.FullName, SshKeyType.RSA, "rsa-comment", length: 2048);

            var info = new SshKeyFileInformation(SshKeyFileSource.FromDisk(privatePath));
            var result = BasicSshKeyFileInformation.FromKeyFileInfo(info);

            result.KeyType.ShouldBe(SshKeyType.RSA);
            result.BitLength.ShouldBe(2048);
            result.Comment.ShouldBe("rsa-comment");
        }
        finally
        {
            dir.Delete(true);
        }
    }

    [AvaloniaTheory]
    [InlineData(256)]
    [InlineData(384)]
    [InlineData(521)]
    public void FromKeyFileInfo_OpenSshEcdsaWithPubFile_ParsesCorrectBitLength(int length)
    {
        var dir = Directory.CreateTempSubdirectory();
        try
        {
            var (privatePath, _) = GenerateOpenSshKeyPair(dir.FullName, SshKeyType.ECDSA, "ecdsa-comment", length: length);

            var info = new SshKeyFileInformation(SshKeyFileSource.FromDisk(privatePath));
            var result = BasicSshKeyFileInformation.FromKeyFileInfo(info);

            result.KeyType.ShouldBe(SshKeyType.ECDSA);
            result.BitLength.ShouldBe(length);
        }
        finally
        {
            dir.Delete(true);
        }
    }

    [AvaloniaFact]
    public void FromKeyFileInfo_EmptyCommentInPubFile_CommentIsEmpty()
    {
        var dir = Directory.CreateTempSubdirectory();
        try
        {
            var (privatePath, _) = GenerateOpenSshKeyPair(dir.FullName, SshKeyType.ED25519, string.Empty);

            var info = new SshKeyFileInformation(SshKeyFileSource.FromDisk(privatePath));
            var result = BasicSshKeyFileInformation.FromKeyFileInfo(info);

            result.Comment.ShouldBeEmpty();
            result.FingerPrint.ShouldNotBeNullOrEmpty();
        }
        finally
        {
            dir.Delete(true);
        }
    }

    [AvaloniaFact]
    public void FromKeyFileInfo_MalformedPubFileContent_ReturnsEmpty()
    {
        var dir = Directory.CreateTempSubdirectory();
        try
        {
            var privatePath = Path.Combine(dir.FullName, "id_bad");
            File.WriteAllText(privatePath, "not a real key");
            File.WriteAllText(privatePath + ".pub", "onlyoneword");

            var info = new SshKeyFileInformation(SshKeyFileSource.FromDisk(privatePath));
            var result = BasicSshKeyFileInformation.FromKeyFileInfo(info);

            result.FingerPrint.ShouldBeEmpty();
            result.Comment.ShouldBeEmpty();
        }
        finally
        {
            dir.Delete(true);
        }
    }

    [AvaloniaFact]
    public void FromKeyFileInfo_PubFileWithInvalidBase64_ReturnsEmpty()
    {
        var dir = Directory.CreateTempSubdirectory();
        try
        {
            var privatePath = Path.Combine(dir.FullName, "id_bad2");
            File.WriteAllText(privatePath, "not a real key");
            File.WriteAllText(privatePath + ".pub", "ssh-ed25519 not-valid-base64!!! comment");

            var info = new SshKeyFileInformation(SshKeyFileSource.FromDisk(privatePath));
            var result = BasicSshKeyFileInformation.FromKeyFileInfo(info);

            result.FingerPrint.ShouldBeEmpty();
        }
        finally
        {
            dir.Delete(true);
        }
    }

    [AvaloniaFact]
    public void FromKeyFileInfo_PuttyV3Key_ParsesCorrectly()
    {
        var dir = Directory.CreateTempSubdirectory();
        try
        {
            var path = GeneratePpkKey(dir.FullName, SshKeyType.RSA, SshKeyFormat.PuTTYv3, "putty-comment");

            var info = new SshKeyFileInformation(SshKeyFileSource.FromDisk(path));
            var result = BasicSshKeyFileInformation.FromKeyFileInfo(info);

            result.Format.ShouldBe(SshKeyFormat.PuTTYv3);
            result.KeyType.ShouldBe(SshKeyType.RSA);
            result.Comment.ShouldBe("putty-comment");
            result.FingerPrint.ShouldNotBeNullOrEmpty();
        }
        finally
        {
            dir.Delete(true);
        }
    }

    [AvaloniaFact]
    public void FromKeyFileInfo_PuttyV2Key_FormatIsPuTTYv2()
    {
        var dir = Directory.CreateTempSubdirectory();
        try
        {
            var path = GeneratePpkKey(dir.FullName, SshKeyType.ED25519, SshKeyFormat.PuTTYv2, "putty2-comment");

            var info = new SshKeyFileInformation(SshKeyFileSource.FromDisk(path));
            var result = BasicSshKeyFileInformation.FromKeyFileInfo(info);

            result.Format.ShouldBe(SshKeyFormat.PuTTYv2);
            result.KeyType.ShouldBe(SshKeyType.ED25519);
            result.Comment.ShouldBe("putty2-comment");
        }
        finally
        {
            dir.Delete(true);
        }
    }

    [AvaloniaFact]
    public void FromKeyFileInfo_EncryptedPuttyKey_StillReadsCommentAndFingerprintFromPlaintextHeader()
    {
        var dir = Directory.CreateTempSubdirectory();
        try
        {
            var path = Path.Combine(dir.FullName, "id_enc.ppk");
            var genInfo = new SshKeyGenerateInfo(SshKeyType.ED25519)
            {
                Comment = "encrypted-comment",
                KeyFormat = SshKeyFormat.PuTTYv3,
                Encryption = new SshKeyEncryptionAes256("s3cr3t", new PuttyV3Encryption())
            };
            SshKey.Generate(path, FileMode.Create, genInfo);

            var info = new SshKeyFileInformation(SshKeyFileSource.FromDisk(path));
            var result = BasicSshKeyFileInformation.FromKeyFileInfo(info);

            result.Comment.ShouldBe("encrypted-comment");
            result.FingerPrint.ShouldNotBeNullOrEmpty();
        }
        finally
        {
            dir.Delete(true);
        }
    }

    [AvaloniaFact]
    public void FromKeyFileInfo_OpenSshPrivateKeyWithoutPubFile_ReturnsEmpty_DueToUnconditionalPubFileLookup()
    {
        // BUG: SshKeyFileInformation.PublicKeyFileName is computed unconditionally for any
        // non-.ppk extension regardless of whether the .pub file physically exists on disk.
        // FromKeyFileInfo only checks "PublicKeyFileName is not null" (not File.Exists), so it
        // always attempts to read the (now missing) .pub file first and swallows the resulting
        // FileNotFoundException as "Empty" instead of falling back to parsing the OpenSSH private
        // key directly. This contradicts the method's own XML doc comment which promises the
        // comment "will be empty ... when no corresponding .pub file is present" (implying the
        // fingerprint/key type would still be parsed) - in reality the whole result is Empty.
        var dir = Directory.CreateTempSubdirectory();
        try
        {
            var (privatePath, publicPath) = GenerateOpenSshKeyPair(dir.FullName, SshKeyType.ED25519, "bob@host");
            File.Delete(publicPath);

            var info = new SshKeyFileInformation(SshKeyFileSource.FromDisk(privatePath));
            var result = BasicSshKeyFileInformation.FromKeyFileInfo(info);

            result.FingerPrint.ShouldBeEmpty();
            result.Comment.ShouldBeEmpty();
            result.BitLength.ShouldBe(0);
        }
        finally
        {
            dir.Delete(true);
        }
    }

    [AvaloniaFact]
    public void ToString_ForSuccessfullyParsedKey_ReturnsEmptyString_DueToInvertedTernary()
    {
        // BUG: BasicSshKeyFileInformation.ToString(SshKeyHashAlgorithmName, string) has an
        // inverted ternary: "IsEmpty ? <formatted string> : string.Empty". This means a
        // *successfully* parsed key (IsEmpty == false) always renders as an empty string, while
        // the *empty/unparseable* placeholder instance renders the formatted placeholder text.
        // This is the exact opposite of the documented "ssh-keygen -lf"-style behaviour.
        var dir = Directory.CreateTempSubdirectory();
        try
        {
            var (privatePath, _) = GenerateOpenSshKeyPair(dir.FullName, SshKeyType.ED25519, "carol@host");
            var info = new SshKeyFileInformation(SshKeyFileSource.FromDisk(privatePath));
            var result = BasicSshKeyFileInformation.FromKeyFileInfo(info);

            result.FingerPrint.ShouldNotBeNullOrEmpty(); // sanity: key was actually parsed
            result.ToString().ShouldBe(string.Empty);
        }
        finally
        {
            dir.Delete(true);
        }
    }

    [AvaloniaFact]
    public void ToString_ForEmptyInstance_ReturnsFormattedPlaceholder_DueToInvertedTernary()
    {
        var empty = new BasicSshKeyFileInformation();

        var text = empty.ToString();

        text.ShouldNotBeNullOrEmpty();
        text.ShouldContain("0");
        text.ShouldContain("SHA256");
    }

    [AvaloniaFact]
    public void Equals_TwoDefaultInstances_AreEqual()
    {
        var a = new BasicSshKeyFileInformation();
        var b = new BasicSshKeyFileInformation();

        a.ShouldBe(b);
        a.GetHashCode().ShouldBe(b.GetHashCode());
    }
}
