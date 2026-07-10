using Avalonia.Headless.XUnit;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Lib.Misc;
using OpenSSH_GUI.SshConfig.Parsers;
using Shouldly;
using Xunit;

namespace OpenSSH_GUI.Tests.Core.Extensions;

public class SshConfigExtensionsTests
{
    [AvaloniaFact]
    public void GetConnectionEntriesFromConfig_HostWithIdentityFile_ReturnsKeyConnectionCredentials()
    {
        const string config = """
                               Host myserver
                                   HostName example.com
                                   User bob
                                   IdentityFile ~/.ssh/id_rsa
                               """;
        var document = SshConfigParser.Parse(config);

        var result = document.GetConnectionEntriesFromConfig().ToArray();

        result.Length.ShouldBe(1);
        var credentials = result[0].ShouldBeOfType<KeyConnectionCredentials>();
        credentials.Hostname.ShouldBe("example.com");
        credentials.Username.ShouldBe("bob");
        credentials.Port.ShouldBe(22);
    }

    [AvaloniaFact]
    public void GetConnectionEntriesFromConfig_HostWithoutIdentityFile_ReturnsPasswordConnectionCredentials()
    {
        const string config = """
                               Host myserver
                                   HostName example.com
                                   User bob
                               """;
        var document = SshConfigParser.Parse(config);

        var result = document.GetConnectionEntriesFromConfig().ToArray();

        result.Length.ShouldBe(1);
        result[0].ShouldBeOfType<PasswordConnectionCredentials>();
    }

    [AvaloniaFact]
    public void GetConnectionEntriesFromConfig_NoHostName_FallsBackToPattern()
    {
        const string config = """
                               Host myserver
                                   User bob
                               """;
        var document = SshConfigParser.Parse(config);

        var result = document.GetConnectionEntriesFromConfig().ToArray();

        result.Length.ShouldBe(1);
        result[0].Hostname.ShouldBe("myserver");
    }

    [AvaloniaFact]
    public void GetConnectionEntriesFromConfig_NoUser_FallsBackToGlobalUser()
    {
        const string config = """
                               User globaluser

                               Host myserver
                                   HostName example.com
                               """;
        var document = SshConfigParser.Parse(config);

        var result = document.GetConnectionEntriesFromConfig().ToArray();

        result.Length.ShouldBe(1);
        result[0].Username.ShouldBe("globaluser");
    }

    [AvaloniaFact]
    public void GetConnectionEntriesFromConfig_NoUserAtAll_FallsBackToEnvironmentUserName()
    {
        const string config = """
                               Host myserver
                                   HostName example.com
                               """;
        var document = SshConfigParser.Parse(config);

        var result = document.GetConnectionEntriesFromConfig().ToArray();

        result.Length.ShouldBe(1);
        result[0].Username.ShouldBe(Environment.UserName);
    }

    [AvaloniaFact]
    public void GetConnectionEntriesFromConfig_NonDefaultPort_IsUsed()
    {
        const string config = """
                               Host myserver
                                   HostName example.com
                                   User bob
                                   Port 2222
                               """;
        var document = SshConfigParser.Parse(config);

        var result = document.GetConnectionEntriesFromConfig().ToArray();

        result.Length.ShouldBe(1);
        result[0].Hostname.ShouldBe("example.com");
        result[0].Port.ShouldBe(2222);
    }

    [AvaloniaFact]
    public void GetConnectionEntriesFromConfig_DefaultPort22_IsNotAppended()
    {
        const string config = """
                               Host myserver
                                   HostName example.com
                                   User bob
                                   Port 22
                               """;
        var document = SshConfigParser.Parse(config);

        var result = document.GetConnectionEntriesFromConfig().ToArray();

        result[0].Hostname.ShouldBe("example.com");
    }

    [AvaloniaFact]
    public void GetConnectionEntriesFromConfig_GlobalPort_UsedWhenHostHasNone()
    {
        const string config = """
                               Port 2200

                               Host myserver
                                   HostName example.com
                                   User bob
                               """;
        var document = SshConfigParser.Parse(config);

        var result = document.GetConnectionEntriesFromConfig().ToArray();

        result[0].Hostname.ShouldBe("example.com");
        result[0].Port.ShouldBe(2200);
        result[0].Username.ShouldBe("bob");
    }

    [AvaloniaFact]
    public void GetConnectionEntriesFromConfig_GlobalIdentityFile_UsedWhenHostHasNone()
    {
        const string config = """
                               IdentityFile ~/.ssh/global_key

                               Host myserver
                                   HostName example.com
                                   User bob
                               """;
        var document = SshConfigParser.Parse(config);

        var result = document.GetConnectionEntriesFromConfig().ToArray();

        result[0].ShouldBeOfType<KeyConnectionCredentials>();
    }

    [AvaloniaTheory]
    [InlineData("Host *\n    User bob")]
    [InlineData("Host ?erver\n    User bob")]
    [InlineData("Host !excluded\n    User bob")]
    public void GetConnectionEntriesFromConfig_WildcardOrNegatedPatterns_AreSkipped(string config)
    {
        var document = SshConfigParser.Parse(config);

        var result = document.GetConnectionEntriesFromConfig().ToArray();

        result.ShouldBeEmpty();
    }

    [AvaloniaFact]
    public void GetConnectionEntriesFromConfig_MultiplePatternsOnOneHostLine_YieldsOneEntryPerPattern()
    {
        const string config = """
                               Host server1 server2
                                   User bob
                               """;
        var document = SshConfigParser.Parse(config);

        var result = document.GetConnectionEntriesFromConfig().ToArray();

        result.Length.ShouldBe(2);
        result.Select(r => r.Hostname).ShouldBe(["server1", "server2"], ignoreOrder: true);
    }

    [AvaloniaFact]
    public void GetConnectionEntriesFromConfig_EmptyDocument_ReturnsEmpty()
    {
        var document = SshConfigParser.Parse(string.Empty);

        var result = document.GetConnectionEntriesFromConfig().ToArray();

        result.ShouldBeEmpty();
    }

    [AvaloniaFact]
    public void GetConnectionEntriesFromConfig_MultipleHosts_ReturnsEntryPerHost()
    {
        const string config = """
                               Host alpha
                                   HostName alpha.example.com
                                   User alice

                               Host beta
                                   HostName beta.example.com
                                   User bob
                                   IdentityFile ~/.ssh/beta_key
                               """;
        var document = SshConfigParser.Parse(config);

        var result = document.GetConnectionEntriesFromConfig().ToArray();

        result.Length.ShouldBe(2);
        result.ShouldContain(r => r.Hostname == "alpha.example.com" && r is PasswordConnectionCredentials);
        result.ShouldContain(r => r.Hostname == "beta.example.com" && r is KeyConnectionCredentials);
    }
}
