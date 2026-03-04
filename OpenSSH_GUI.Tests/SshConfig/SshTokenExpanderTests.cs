using OpenSSH_GUI.SshConfig;
using Shouldly;
using Xunit;

namespace OpenSSH_GUI.Tests.SshConfig;

public class SshTokenExpanderTests
{
    [Fact]
    public void Expand_NoTokens_ShouldReturnOriginalString()
    {
        var context = new SshTokenContext();
        var result = SshTokenExpander.Expand("nothing to see here", context);
        result.ShouldBe("nothing to see here");
    }

    [Fact]
    public void Expand_PercentPercent_ShouldReturnSinglePercent()
    {
        var context = new SshTokenContext();
        var result = SshTokenExpander.Expand("100%% sure", context);
        result.ShouldBe("100% sure");
    }

    [Fact]
    public void Expand_CommonTokens_ShouldSubstituteCorrectly()
    {
        var context = new SshTokenContext(
            RemoteHostname: "remote.host",
            RemoteUser: "alice",
            Port: 2222,
            LocalUser: "bob"
        );

        SshTokenExpander.Expand("ssh://%r@%h:%p", context).ShouldBe("ssh://alice@remote.host:2222");
        SshTokenExpander.Expand("user is %u", context).ShouldBe("user is bob");
    }

    [Fact]
    public void Expand_UnrecognizedToken_ShouldKeepUnchanged()
    {
        var context = new SshTokenContext();
        var result = SshTokenExpander.Expand("token %z is unknown", context);
        result.ShouldBe("token %z is unknown");
    }

    [Fact]
    public void Expand_TrailingPercent_ShouldKeepUnchanged()
    {
        var context = new SshTokenContext();
        var result = SshTokenExpander.Expand("ends with %", context);
        result.ShouldBe("ends with %");
    }

    [Fact]
    public void Expand_AllTokens_ShouldSubstituteCorrectly()
    {
        var context = new SshTokenContext(
            RemoteHostname: "h",
            LocalHostname: "H",
            OriginalHostname: "n",
            Port: 123,
            RemoteUser: "r",
            LocalUser: "u",
            LocalUserId: 1000,
            LocalHostnameFqdn: "l",
            LocalHostnameShort: "L",
            LocalHomeDirectory: "d",
            HostKeyAlias: "k",
            ConnectionHash: "C",
            ProxySocketPath: "T"
        );

        SshTokenExpander.Expand("%C %d %h %H %i %k %l %L %n %p %r %T %u %U", context)
            .ShouldBe("C d h H 1000 k l L n 123 r T u 1000");
    }
}
