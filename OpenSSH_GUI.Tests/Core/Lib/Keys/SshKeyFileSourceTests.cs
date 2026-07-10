using Avalonia.Headless.XUnit;
using OpenSSH_GUI.Core.Lib.Keys;
using Shouldly;
using Xunit;

namespace OpenSSH_GUI.Tests.Core.Lib.Keys;

public class SshKeyFileSourceTests
{
    [AvaloniaFact]
    public void FromDisk_SetsAbsolutePathAndNotProvidedByConfig()
    {
        var source = SshKeyFileSource.FromDisk("/tmp/id_rsa");

        source.AbsolutePath.ShouldBe("/tmp/id_rsa");
        source.ProvidedByConfig.ShouldBeFalse();
    }

    [AvaloniaFact]
    public void FromConfig_SetsAbsolutePathAndProvidedByConfig()
    {
        var source = SshKeyFileSource.FromConfig("/tmp/id_ed25519");

        source.AbsolutePath.ShouldBe("/tmp/id_ed25519");
        source.ProvidedByConfig.ShouldBeTrue();
    }

    [AvaloniaFact]
    public void Default_HasEmptyPathAndNotProvidedByConfig()
    {
        var source = new SshKeyFileSource();

        source.AbsolutePath.ShouldBe(string.Empty);
        source.ProvidedByConfig.ShouldBeFalse();
    }

    [AvaloniaFact]
    public void ToString_ContainsPathAndConfigFlag()
    {
        var source = SshKeyFileSource.FromConfig("/tmp/key");

        source.ToString().ShouldBe("/tmp/key | Referenced by Config: True");
    }

    [AvaloniaFact]
    public void Equality_SameValues_AreEqual()
    {
        var a = SshKeyFileSource.FromDisk("/tmp/key");
        var b = SshKeyFileSource.FromDisk("/tmp/key");

        a.ShouldBe(b);
        (a == b).ShouldBeTrue();
    }

    [AvaloniaFact]
    public void Equality_DifferentProvidedByConfig_AreNotEqual()
    {
        var a = SshKeyFileSource.FromDisk("/tmp/key");
        var b = SshKeyFileSource.FromConfig("/tmp/key");

        a.ShouldNotBe(b);
    }

    [AvaloniaFact]
    public void Equality_DifferentPath_AreNotEqual()
    {
        var a = SshKeyFileSource.FromDisk("/tmp/key1");
        var b = SshKeyFileSource.FromDisk("/tmp/key2");

        a.ShouldNotBe(b);
    }
}
