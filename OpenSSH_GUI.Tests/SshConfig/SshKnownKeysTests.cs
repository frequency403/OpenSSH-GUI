using Avalonia.Headless.XUnit;
using OpenSSH_GUI.SshConfig.Models;
using Shouldly;
using Xunit;

namespace OpenSSH_GUI.Tests.SshConfig;

public class SshKnownKeysTests
{
    [AvaloniaTheory, InlineData("hostname", "HostName"), InlineData("USER", "User"), InlineData("identityfile", "IdentityFile"), InlineData("UNKNOWN", "UNKNOWN")]
    public void Normalize_ShouldCanonicalizeCasing(string input, string expected) { SshKnownKeys.Normalize(input).ShouldBe(expected); }

    [AvaloniaTheory, InlineData("IdentityFile", true), InlineData("HostName", false)]
    public void IsMultiOccurrenceKey_Tests(string key, bool expected) { SshKnownKeys.IsMultiOccurrenceKey(key).ShouldBe(expected); }

    [AvaloniaTheory, InlineData("SendEnv", true), InlineData("HostName", false)]
    public void IsMultiTokenKey_Tests(string key, bool expected) { SshKnownKeys.IsMultiTokenKey(key).ShouldBe(expected); }

    [AvaloniaTheory, InlineData("HostName", true), InlineData("SomethingRandom", false)]
    public void IsKnownKey_Tests(string key, bool expected) { SshKnownKeys.IsKnownKey(key).ShouldBe(expected); }
}