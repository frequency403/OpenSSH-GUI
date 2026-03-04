using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Extensions;
using Shouldly;
using Xunit;

namespace OpenSSH_GUI.Tests.Core.Extensions;

public class KeyTypeExtensionTests
{
    [Theory]
    [InlineData(KeyType.RSA, new[] { 1024, 2048, 3072, 4096 })]
    [InlineData(KeyType.ECDSA, new[] { 256, 384, 521 })]
    [InlineData(KeyType.ED25519, new int[0])]
    public void GetBitValues_Tests(KeyType type, int[] expected)
    {
        type.GetBitValues().ShouldBe(expected);
    }

    [Fact]
    public void GetAvailableKeyTypes_Tests()
    {
        var types = KeyTypeExtension.GetAvailableKeyTypes();
        types.ShouldNotBeEmpty();
        types.Count().ShouldBe(Enum.GetValues<KeyType>().Length);
    }
}
