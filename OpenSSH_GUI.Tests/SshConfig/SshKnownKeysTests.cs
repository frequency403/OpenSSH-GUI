using OpenSSH_GUI.SshConfig.Models;
using Shouldly;
using Xunit;

namespace OpenSSH_GUI.Tests.SshConfig;

public class SshKnownKeysTests
{
    [Theory, InlineData("hostname", "HostName"), InlineData("USER", "User"), InlineData("identityfile", "IdentityFile"), InlineData("UNKNOWN", "UNKNOWN")]
    public void Normalize_ShouldCanonicalizeCasing(string input, string expected) { SshKnownKeys.Normalize(input).ShouldBe(expected); }

    [Theory, InlineData("IdentityFile", true), InlineData("HostName", false)]
    public void IsMultiOccurrenceKey_Tests(string key, bool expected) { SshKnownKeys.IsMultiOccurrenceKey(key).ShouldBe(expected); }

    [Theory, InlineData("SendEnv", true), InlineData("HostName", false)]
    public void IsMultiTokenKey_Tests(string key, bool expected) { SshKnownKeys.IsMultiTokenKey(key).ShouldBe(expected); }

    [Theory, InlineData("HostName", true), InlineData("SomethingRandom", false)]
    public void IsKnownKey_Tests(string key, bool expected) { SshKnownKeys.IsKnownKey(key).ShouldBe(expected); }
}