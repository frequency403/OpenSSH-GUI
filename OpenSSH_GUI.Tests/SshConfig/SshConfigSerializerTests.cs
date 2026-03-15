using OpenSSH_GUI.SshConfig.Models;
using OpenSSH_GUI.SshConfig.Options;
using OpenSSH_GUI.SshConfig.Parsers;
using OpenSSH_GUI.SshConfig.Serializers;
using Shouldly;
using Xunit;

namespace OpenSSH_GUI.Tests.SshConfig;

public class SshConfigSerializerTests
{
    private const string MyConfig = """
                                    # ── Arbeit: GitHub ──────────────────────────────────────────
                                    Host github-work
                                        HostName github.com
                                        User git
                                        IdentityFile ~/keys/work/github_ed25519
                                        IdentitiesOnly yes

                                    # ── Privat: GitHub ──────────────────────────────────────────
                                    Host github-personal
                                        HostName github.com
                                        User git
                                        IdentityFile /home/alice/projects/personal/ssh/id_ed25519
                                        IdentitiesOnly yes

                                    # ── Produktionsserver ───────────────────────────────────────
                                    Host prod-server
                                        HostName 203.0.113.42
                                        User deploy
                                        Port 2222
                                        IdentityFile /etc/ssh/keys/production/deploy_rsa
                                        IdentitiesOnly yes
                                        StrictHostKeyChecking yes

                                    # ── Staging-Server ──────────────────────────────────────────
                                    Host staging
                                        HostName staging.example.com
                                        User ubuntu
                                        IdentityFile ~/projects/infra/keys/staging_ed25519
                                        IdentitiesOnly yes

                                    # ── Jump-Host / Bastion ─────────────────────────────────────
                                    Host bastion
                                        HostName bastion.example.com
                                        User ops
                                        IdentityFile /opt/secrets/ssh/bastion_key
                                        IdentitiesOnly yes

                                    # ── Interner Server via Jump-Host ───────────────────────────
                                    Host internal-db
                                        HostName 10.0.1.50
                                        User dbadmin
                                        ProxyJump bastion
                                        IdentityFile /opt/secrets/ssh/internal_ed25519
                                        IdentitiesOnly yes

                                    # ── Raspberry Pi im Heimnetz ────────────────────────────────
                                    Host raspi
                                        HostName 192.168.1.100
                                        User pi
                                        IdentityFile ~/home-lab/keys/raspi_ed25519
                                        IdentitiesOnly yes

                                    # ── Fallback für alle anderen Hosts ─────────────────────────
                                    Host *
                                        ServerAliveInterval 60
                                        ServerAliveCountMax 3
                                        AddKeysToAgent yes
                                    """;

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
    public void Serialize_UnsupportedBlockType_ShouldThrow()
    {
        // Arrange
        var mockBlock = new UnsupportedBlock();
        var doc = new SshConfigDocument([], [mockBlock]);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => SshConfigSerializer.Serialize(doc));
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

    private sealed record UnsupportedBlock : SshBlock
    {
        public UnsupportedBlock() : base([], 0, "", null)
        {
        }
    }
}