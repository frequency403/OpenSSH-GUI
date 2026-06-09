using OpenSSH_GUI.Core.Extensions;
using SshNet.Keygen;
using Xunit;

namespace OpenSSH_GUI.Tests.Core.Extensions;

public class SshKeyTypeExtensionTests
{
    [Theory, InlineData(SshKeyType.RSA), InlineData(SshKeyType.ECDSA), InlineData(SshKeyType.ED25519)]
    public static void SshKeyType_Tests(SshKeyType sshKeyType)
    {
        var bitValues = sshKeyType.SupportedKeySizes;
        Assert.NotEmpty(bitValues);
    }
}