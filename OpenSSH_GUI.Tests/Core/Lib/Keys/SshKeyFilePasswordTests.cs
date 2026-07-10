using System.Text;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using OpenSSH_GUI.Core.Lib.Keys;
using Shouldly;
using SshNet.Keygen.SshKeyEncryption;
using Xunit;

namespace OpenSSH_GUI.Tests.Core.Lib.Keys;

public class SshKeyFilePasswordTests
{
    [AvaloniaFact]
    public void Default_IsNotValid_AndSpanIsEmpty()
    {
        using var password = new SshKeyFilePassword();

        password.IsValid.ShouldBeFalse();
        password.WrittenSpan.Length.ShouldBe(0);
        password.GetPasswordString().ShouldBe(string.Empty);
    }

    [AvaloniaFact]
    public void Set_WithBytes_BecomesValidAndStoresBytes()
    {
        using var password = new SshKeyFilePassword();

        password.Set("hunter2"u8);
        

        password.IsValid.ShouldBeTrue();
        password.WrittenSpan.ToArray().ShouldBe("hunter2"u8.ToArray());
        password.GetPasswordString().ShouldBe("hunter2");
    }

    [AvaloniaFact]
    public void Set_WithCustomEncoding_DecodesUsingThatEncoding()
    {
        using var password = new SshKeyFilePassword();
        var bytes = Encoding.Unicode.GetBytes("s3cr3t");

        password.Set(bytes, Encoding.Unicode);
        

        password.GetPasswordString().ShouldBe("s3cr3t");
    }

    [AvaloniaFact]
    public void Set_CalledTwice_OverwritesPreviousValue()
    {
        using var password = new SshKeyFilePassword();

        password.Set("first"u8);
        password.Set("second"u8);
        

        password.GetPasswordString().ShouldBe("second");
    }

    [AvaloniaFact]
    public void Clear_ResetsToEmptyState()
    {
        using var password = new SshKeyFilePassword();
        password.Set("hunter2"u8);
        

        password.Clear();
        

        password.IsValid.ShouldBeFalse();
        password.WrittenSpan.Length.ShouldBe(0);
        password.GetPasswordString().ShouldBe(string.Empty);
    }

    [AvaloniaFact]
    public void Set_EmptyBytes_DoesNotBecomeValid()
    {
        using var password = new SshKeyFilePassword();

        password.Set(ReadOnlySpan<byte>.Empty);
        

        password.IsValid.ShouldBeFalse();
    }

    [AvaloniaFact]
    public void ToSshKeyEncryption_WhenInvalid_ReturnsDefaultSshKeyEncryption()
    {
        using var password = new SshKeyFilePassword();

        var encryption = password.ToSshKeyEncryption();

        encryption.ShouldBeOfType<SshKeyEncryptionNone>();
    }

    [AvaloniaFact]
    public void ToSshKeyEncryption_WhenValid_ReturnsAes256Encryption()
    {
        using var password = new SshKeyFilePassword();
        password.Set("hunter2"u8);
        

        var encryption = password.ToSshKeyEncryption();

        encryption.ShouldBeOfType<SshKeyEncryptionAes256>();
    }

    [AvaloniaFact]
    public void Dispose_ClearsBuffer()
    {
        var password = new SshKeyFilePassword();
        password.Set("hunter2"u8);
        

        password.Dispose();

        password.WrittenSpan.Length.ShouldBe(0);
    }

    [AvaloniaFact]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        var password = new SshKeyFilePassword();
        password.Set("hunter2"u8);
        

        password.Dispose();
        Should.NotThrow(() => password.Dispose());
    }
}
