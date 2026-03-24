using OpenSSH_GUI.Core.Extensions;
using Shouldly;
using SshNet.Keygen;
using Xunit;

namespace OpenSSH_GUI.Tests.Core.Extensions;

public class SshKeyFormatExtensionTests
{
    [Theory]
    [InlineData(SshKeyFormat.OpenSSH, true, ".pub")]
    [InlineData(SshKeyFormat.OpenSSH, false, null)]
    [InlineData(SshKeyFormat.PuTTYv2, false, ".ppk")]
    [InlineData(SshKeyFormat.PuTTYv3, true, ".ppk")]
    public void GetExtension_Tests(SshKeyFormat format, bool isPublic, string? expected)
    {
        format.GetExtension(isPublic).ShouldBe(expected);
    }

    [Theory]
    [InlineData(SshKeyFormat.OpenSSH, "test.key", true, "test.pub")]
    [InlineData(SshKeyFormat.PuTTYv3, "test.key", false, "test.ppk")]
    public void ChangeExtension_Tests(SshKeyFormat format, string path, bool isPublic, string expected)
    {
        format.ChangeExtension(path, isPublic).ShouldBe(expected);
    }
}