using Microsoft.Extensions.Configuration;
using OpenSSH_GUI.SshConfig;
using Shouldly;
using Xunit;

namespace OpenSSH_GUI.Tests.SshConfig;

public class SshConfigurationBindingTests
{
    [Fact]
    public void AddSshConfig_ShouldBeBindableToObjects()
    {
        // Arrange
        var configContent =
            "Port 2222\nUser globaluser\n\nHost server1\n    Port 2222\n    User serveruser\n\nHost server2\n    Port 3333";
        var filePath = Path.GetTempFileName();
        File.WriteAllText(filePath, configContent);

        try
        {
            var builder = new ConfigurationBuilder();
            builder.AddSshConfig(filePath);
            var configuration = builder.Build();

            // Act
            var sshConfig = configuration.GetSection("SshConfig").Get<SshConfiguration>();

            // Assert
            sshConfig.ShouldNotBeNull();
            sshConfig.Global.ShouldNotBeNull();
            sshConfig.Global.Port.ShouldBe(2222);
            sshConfig.Global.User.ShouldBe("globaluser");

            sshConfig.Hosts.ShouldNotBeNull();
            sshConfig.Hosts.Count.ShouldBe(2);

            sshConfig.Hosts[0].Patterns.ShouldNotBeEmpty();
            sshConfig.Hosts[0].Patterns[0].ShouldBe("server1");
            sshConfig.Hosts[0].Port.ShouldBe(2222);
            sshConfig.Hosts[0].User.ShouldBe("serveruser");

            sshConfig.Hosts[1].Patterns.ShouldNotBeEmpty();
            sshConfig.Hosts[1].Patterns[0].ShouldBe("server2");
            sshConfig.Hosts[1].Port.ShouldBe(3333);
        }
        finally
        {
            if (File.Exists(filePath)) File.Delete(filePath);
        }
    }
}