using System.Collections.Immutable;
using OpenSSH_GUI.SshConfig.Extensions;
using OpenSSH_GUI.SshConfig.Models;
using Xunit;

namespace OpenSSH_GUI.Tests.SshConfig;

public class SshHostSettingsTests
{
    [Fact]
    public void GetSettings_ShouldMapCommonEntries()
    {
        // Arrange
        var items = ImmutableArray.Create<SshLineItem>(
            SshConfigEntry.Create("HostName", "example.com"),
            SshConfigEntry.Create("User", "alice"),
            SshConfigEntry.Create("Port", "2222"),
            SshConfigEntry.Create("IdentityFile", "~/.ssh/id_rsa"),
            SshConfigEntry.Create("IdentityFile", "~/.ssh/id_ed25519"),
            SshConfigEntry.Create("ProxyJump", "jump.example.com"),
            SshConfigEntry.Create("LocalForward", "8080", "localhost:80"),
            SshConfigEntry.Create("Compression", "yes")
        );
        var block = SshHostBlock.Create(["myserver"], items);

        // Act
        var settings = block.GetSettings();

        // Assert
        Assert.Equal("example.com", settings.HostName);
        Assert.Equal("alice", settings.User);
        Assert.Equal(2222, settings.Port);
        Assert.Equal("jump.example.com", settings.ProxyJump);
        Assert.Equal(2, settings.IdentityFiles?.Length);
        Assert.Contains("~/.ssh/id_rsa", settings.IdentityFiles ?? []);
        Assert.Contains("~/.ssh/id_ed25519", settings.IdentityFiles ?? []);
        Assert.Single(settings.LocalForwards ?? []);
        Assert.Equal("8080 localhost:80", settings.LocalForwards?[0]);
        Assert.Single(settings.OtherEntries ?? []);
        Assert.Equal("Compression", settings.OtherEntries?[0].Key);
    }

    [Fact]
    public void WithSettings_ShouldUpdateBlock()
    {
        // Arrange
        var block = SshHostBlock.Create(["myserver"]);
        var settings = new SshHostSettings(
            ["myserver"],
            "new.example.com",
            "bob",
            22,
            ["~/.ssh/id_new"],
            LocalForwards: ["9000 localhost:90"]
        );

        // Act
        var updatedBlock = block.WithSettings(settings);
        var reserializedSettings = updatedBlock.GetSettings();

        // Assert
        Assert.Equal("new.example.com", reserializedSettings.HostName);
        Assert.Equal("bob", reserializedSettings.User);
        Assert.Equal(22, reserializedSettings.Port);
        Assert.Single(reserializedSettings.IdentityFiles);
        Assert.Equal("~/.ssh/id_new", reserializedSettings.IdentityFiles[0]);
        Assert.Single(reserializedSettings.LocalForwards);
        Assert.Equal("9000 localhost:90", reserializedSettings.LocalForwards[0]);
    }

    [Fact]
    public void EmptySettings_ShouldBeCorrect()
    {
        // Arrange & Act
        var settings = SshHostSettings.Empty;

        // Assert
        Assert.True(settings.IsEmpty);
        Assert.Null(settings.HostName);
        Assert.Null(settings.User);
        Assert.Null(settings.Port);
        Assert.True(settings.IdentityFiles == null || settings.IdentityFiles.Length == 0);
    }
}