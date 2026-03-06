using OpenSSH_GUI.SshConfig;
using Shouldly;
using Xunit;

namespace OpenSSH_GUI.Tests.SshConfig;

public class SshWildcardMatcherTests
{
    [Theory]
    [InlineData("example.com", "example.com", true)]
    [InlineData("example.com", "*.com", true)]
    [InlineData("example.com", "example.*", true)]
    [InlineData("example.com", "*example*", true)]
    [InlineData("example.com", "ex?mple.com", true)]
    [InlineData("example.com", "other.com", false)]
    [InlineData("abc", "a?c", true)]
    [InlineData("abc", "a*", true)]
    [InlineData("abc", "*c", true)]
    [InlineData("abc", "*", true)]
    [InlineData("abc", "abcd", false)]
    [InlineData("abc", "ab", false)]
    public void MatchesGlob_Tests(string input, string pattern, bool expected)
    {
        SshWildcardMatcher.MatchesGlob(input.AsSpan(), pattern.AsSpan()).ShouldBe(expected);
    }

    [Theory]
    [InlineData("host1", new[] { "host1", "host2" }, true)]
    [InlineData("host2", new[] { "host1", "host2" }, true)]
    [InlineData("host3", new[] { "host1", "host2" }, false)]
    [InlineData("host1", new[] { "!host1", "host*" }, false)]
    [InlineData("host2", new[] { "!host1", "host*" }, true)]
    [InlineData("host1", new[] { "host*", "!host1" }, false)]
    public void Matches_Tests(string hostname, string[] patterns, bool expected)
    {
        SshWildcardMatcher.Matches(hostname.AsSpan(), patterns).ShouldBe(expected);
    }
}