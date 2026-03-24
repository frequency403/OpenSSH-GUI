using OpenSSH_GUI.SshConfig.Models;
using OpenSSH_GUI.SshConfig.Options;
using OpenSSH_GUI.SshConfig.Parsers;
using OpenSSH_GUI.SshConfig.Serializers;
using Shouldly;
using Xunit;

namespace OpenSSH_GUI.Tests.SshConfig;

public class SshConfigSerializerTests
{
    [Fact]
    public void Serialize_SimpleDocument_ShouldProduceCorrectOutput()
    {
        var doc = new SshConfigDocument(
            [SshConfigEntry.Create("VisualHostKey", "yes")],
            [SshHostBlock.Create(["example"], [SshConfigEntry.Create("User", "alice")])]
        );

        var output = SshConfigSerializer.Serialize(doc, new SshSerializerOptions { Indent = "    " });

        output.ShouldContain("VisualHostKey yes");
        output.ShouldContain("Host example");
        output.ShouldContain("    User alice");
    }

    [Fact]
    public void Serialize_RoundTrip_ShouldPreserveFormatting()
    {
        var input =
            "# Global comment\nVisualHostKey yes\n\nHost example\n    User alice\n    # item comment\n    Port 22";
        var doc = SshConfigParser.Parse(input);
        var output = SshConfigSerializer.Serialize(doc, SshSerializerOptions.RoundTripMode);

        output.Replace("\r\n", "\n").Trim().ShouldBe(input.Replace("\r\n", "\n").Trim());
    }

    [Fact]
    public void Serialize_MatchBlock_ShouldProduceCorrectOutput()
    {
        var criteria = new[] { SshMatchCriterion.ForHost("example.com"), SshMatchCriterion.ForUser("root") };
        var doc = new SshConfigDocument(
            [],
            [SshMatchBlock.Create(criteria, [SshConfigEntry.Create("Port", "22")])]
        );

        var output = SshConfigSerializer.Serialize(doc);
        output.ShouldContain("Match host example.com user root");
    }

    [Fact]
    public void Serialize_CleanMode_WithOptions_ShouldFormatCorrectly()
    {
        // Arrange
        var doc = new SshConfigDocument(
            [],
            [SshHostBlock.Create(["example"], [SshConfigEntry.Create("User", "alice")])]
        );
        var options = new SshSerializerOptions
        {
            Indent = "\t",
            KeyValueSeparator = "=",
            NewLine = "\n",
            BlankLineBetweenBlocks = true
        };

        // Act
        var output = SshConfigSerializer.Serialize(doc, options);

        // Assert
        Assert.Equal("Host example\n\tUser=alice\n", output);
    }

    [Fact]
    public void Serialize_RoundTrip_WithModifications_ShouldRegenerate()
    {
        // Arrange
        var original = "Host example\n  User alice\n";
        var doc = SshConfigParser.Parse(original);
        var block = doc.HostBlocks.First();
        var entry = block.GetEntries("User").First();

        // Modify entry and clear RawText to force regeneration
        var modifiedEntry = entry with { Values = ["bob"], RawText = string.Empty };
        var modifiedBlock = block with { Items = [modifiedEntry], RawHeaderText = string.Empty };
        var modifiedDoc = doc with { Blocks = [modifiedBlock] };

        var options = new SshSerializerOptions { RoundTrip = true };

        // Act
        var output = SshConfigSerializer.Serialize(modifiedDoc, options);

        // Assert
        Assert.Contains("Host example", output);
        Assert.Contains("User bob", output);
        Assert.DoesNotContain("User alice", output);
    }

    [Fact]
    public void QuoteIfNeeded_ShouldQuoteWhitespace()
    {
        // Arrange
        var entry = SshConfigEntry.Create("IdentityFile", "/path/with space/id_rsa");

        // Act
        var output = SshConfigSerializer.Serialize(new SshConfigDocument([], [SshHostBlock.Create(["ex"], [entry])]));

        // Assert
        Assert.Contains("\"/path/with space/id_rsa\"", output);
    }
}