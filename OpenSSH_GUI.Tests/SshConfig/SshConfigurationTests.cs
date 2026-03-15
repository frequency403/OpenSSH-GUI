using Microsoft.Extensions.Configuration;
using OpenSSH_GUI.SshConfig.Extensions;
using Shouldly;
using Xunit;

namespace OpenSSH_GUI.Tests.SshConfig;

public class SshConfigurationTests
{
    [Fact]
    public void AddSshConfig_ShouldLoadGlobalEntries()
    {
        // Arrange
        var configContent = "Port 2222\nUser testuser";
        var filePath = Path.GetTempFileName();
        File.WriteAllText(filePath, configContent);

        try
        {
            var builder = new ConfigurationBuilder();

            // Act
            builder.AddSshConfig(filePath);
            var configuration = builder.Build();

            // Assert
            configuration["SshConfig:Global:Port"].ShouldBe("2222");
            configuration["SshConfig:Global:User"].ShouldBe("testuser");
        }
        finally
        {
            if (File.Exists(filePath)) File.Delete(filePath);
        }
    }

    [Fact]
    public void AddSshConfig_ShouldLoadHostBlocks()
    {
        // Arrange
        var configContent = "Host server1\n    Port 2222\n\nHost server2\n    Port 3333";
        var filePath = Path.GetTempFileName();
        File.WriteAllText(filePath, configContent);

        try
        {
            var builder = new ConfigurationBuilder();

            // Act
            builder.AddSshConfig(filePath);
            var configuration = builder.Build();

            // Assert
            configuration["SshConfig:Hosts:0:Patterns:0"].ShouldBe("server1");
            configuration["SshConfig:Hosts:0:Port"].ShouldBe("2222");

            configuration["SshConfig:Hosts:1:Patterns:0"].ShouldBe("server2");
            configuration["SshConfig:Hosts:1:Port"].ShouldBe("3333");
        }
        finally
        {
            if (File.Exists(filePath)) File.Delete(filePath);
        }
    }

    [Fact]
    public void AddSshConfig_Optional_ShouldNotThrowIfFileMissing()
    {
        // Arrange
        var builder = new ConfigurationBuilder();

        // Act & Assert
        Should.NotThrow(() =>
        {
            builder.AddSshConfig("nonexistent_file", true);
            builder.Build();
        });
    }
}