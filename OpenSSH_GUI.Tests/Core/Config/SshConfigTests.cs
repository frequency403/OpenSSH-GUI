using OpenSSH_GUI.Core.Lib.Config;
using Shouldly;
using Xunit;

namespace OpenSSH_GUI.Tests.Core.Config;

public class SshConfigTests
{
    [Fact]
    public void Parse_SimpleConfig_ShouldReturnExpectedValues()
    {
        var content = @"Host myhost
    HostName 1.2.3.4
    User myuser
    Port 2222
    UnknownKey SomeValue";

        var config = SshConfigParser.Parse(content);

        config.HostEntries.Count.ShouldBe(1);
        var entry = config.HostEntries[0];
        entry.Host.ShouldBe("myhost");
        entry.HostName.ShouldBe("1.2.3.4");
        entry.User.ShouldBe("myuser");
        entry.Port.ShouldBe(2222);
        entry.AdditionalProperties["UnknownKey"].ShouldBe("SomeValue");
    }

    [Fact]
    public void Write_SimpleConfig_ShouldProduceCorrectOutput()
    {
        var config = new OpenSSH_GUI.Core.Lib.Config.SshConfig();
        var entry = new SshHostEntry
        {
            Host = "example",
            HostName = "127.0.0.1",
            User = "root"
        };
        entry.AdditionalProperties["ForwardAgent"] = "yes";
        config.HostEntries.Add(entry);

        var output = SshConfigParser.Write(config);

        output.ShouldContain("Host example");
        output.ShouldContain("HostName 127.0.0.1");
        output.ShouldContain("User root");
        output.ShouldContain("ForwardAgent yes");
    }
}
