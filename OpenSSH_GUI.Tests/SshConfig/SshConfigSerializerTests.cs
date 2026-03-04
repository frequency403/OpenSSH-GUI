using OpenSSH_GUI.SshConfig;
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
        var input = "# Global comment\nVisualHostKey yes\n\nHost example\n    User alice\n    # item comment\n    Port 22";
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
}
