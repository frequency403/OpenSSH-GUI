using System.Reflection;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.SshConfig;
using Shouldly;
using Xunit;

namespace OpenSSH_GUI.Tests.SshConfig;

public class SshConfigParserTests
{
    private string GetEmbeddedResource(string fileName)
    {
        var assembly = typeof(SshConfigParserTests).Assembly;
        var resourceName = $"OpenSSH_GUI.Tests.Assets.Testfiles.{fileName}";
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            throw new Exception($"Resource {resourceName} not found. Available: {string.Join(", ", assembly.GetManifestResourceNames())}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    [Fact]
    public void Parse_GlobalConfig_ShouldParseEmbeddedFile()
    {
        var content = GetEmbeddedResource("ssh_config_global");
        var doc = SshConfigParser.Parse(content);
        
        doc.Blocks.Length.ShouldBeGreaterThan(0);
        // "Host *" sollte vorhanden sein
        var allHosts = doc.Blocks.OfType<SshHostBlock>().ToList();
        allHosts.ShouldContain(b => b.Patterns.Contains("*"));
        
        // Suche nach "ConnectTimeout 20" im globalen Kontext oder im Host * Block
        var globalEntries = doc.GetGlobalEntries().ToArray();
        var hostStar = allHosts.FirstOrDefault(b => b.Patterns.Contains("*"));
        hostStar.ShouldNotBeNull();
        hostStar.GetEntries().ShouldContain(e => e.Key == "ConnectTimeout" && e.Value == "20");
    }

    [Fact]
    public void Parse_PersonalConfig_ShouldParseEmbeddedFile()
    {
        var content = GetEmbeddedResource("ssh_config_personal");
        var doc = SshConfigParser.Parse(content);
        
        doc.Blocks.Length.ShouldBeGreaterThan(1);
        var allHosts = doc.Blocks.OfType<SshHostBlock>().ToList();
        
        // Host prod-web-01
        var prodWeb01 = allHosts.FirstOrDefault(b => b.Patterns.Contains("prod-web-01"));
        prodWeb01.ShouldNotBeNull();
        prodWeb01.GetEntries().ShouldContain(e => e.Key == "HostName" && e.Value == "10.10.1.11");
    }

    [Fact]
    public void Parse_SshdServerConfig_ShouldParseEmbeddedFile()
    {
        var content = GetEmbeddedResource("sshd_config_server");
        var doc = SshConfigParser.Parse(content);
        
        // sshd_config hat normalerweise keine Host-Blöcke (außer Match)
        var globalEntries = doc.GetGlobalEntries().ToArray();
        globalEntries.ShouldContain(e => e.Key == "Port" && e.Value == "22");
        globalEntries.ShouldContain(e => e.Key == "AddressFamily" && e.Value == "inet");
        
        // Match-Blöcke
        var matchBlocks = doc.Blocks.OfType<SshMatchBlock>().ToList();
        matchBlocks.ShouldNotBeEmpty();
        var userDeploy = matchBlocks.FirstOrDefault(b => b.Criteria.Any(c => c.Kind == SshMatchCriterionKind.User && c.Pattern == "deploy"));
        userDeploy.ShouldNotBeNull();
        userDeploy.GetEntries().ShouldContain(e => e.Key == "AllowTcpForwarding" && e.Value == "no");
    }

    [Fact]
    public void Parse_EmptyContent_ShouldReturnEmptyDocument()
    {
        var doc = SshConfigParser.Parse("");
        doc.GlobalItems.ShouldAllBe(i => i is SshBlankLine);
        doc.Blocks.ShouldBeEmpty();
    }

    [Fact]
    public void Parse_SimpleHostBlock_ShouldParseCorrectly()
    {
        var content = @"
Host example
    HostName 1.2.3.4
    User alice
";
        var doc = SshConfigParser.Parse(content);
        doc.Blocks.Length.ShouldBe(1);
        var hostBlock = doc.Blocks[0].ShouldBeOfType<SshHostBlock>();
        hostBlock.Patterns.ShouldContain("example");

        var entries = hostBlock.GetEntries().ToArray();
        entries.Length.ShouldBe(2);
        entries[0].Key.ShouldBe("HostName");
        entries[0].Value.ShouldBe("1.2.3.4");
        entries[1].Key.ShouldBe("User");
        entries[1].Value.ShouldBe("alice");
    }

    [Fact]
    public void Parse_GlobalItems_ShouldParseCorrectly()
    {
        var content = @"
VisualHostKey yes
ForwardAgent no

Host example
    User alice
";
        var doc = SshConfigParser.Parse(content);
        var globalEntries = doc.GetGlobalEntries().ToArray();
        globalEntries.Length.ShouldBe(2);
        globalEntries[0].Key.ShouldBe("VisualHostKey");
        globalEntries[1].Key.ShouldBe("ForwardAgent");
        doc.Blocks.Length.ShouldBe(1);
    }

    [Fact]
    public void Parse_Comments_ShouldBePreserved()
    {
        var content = @"# Global comment
VisualHostKey yes # inline global

Host example # host comment
    # item comment
    User alice
";
        var doc = SshConfigParser.Parse(content);
        doc.GlobalItems[0].ShouldBeOfType<SshCommentLine>().Comment.ShouldBe("# Global comment");

        var entry = doc.GlobalItems[1].ShouldBeOfType<SshConfigEntry>();
        entry.InlineComment.ShouldBe("# inline global");

        var hostBlock = doc.Blocks[0].ShouldBeOfType<SshHostBlock>();
        hostBlock.HeaderComment.ShouldBe("# host comment");
    }

    [Fact]
    public void Parse_MultipleHosts_ShouldParseCorrectly()
    {
        var content = "Host host1 host2\n  User bob";
        var doc = SshConfigParser.Parse(content);
        var hostBlock = doc.Blocks[0].ShouldBeOfType<SshHostBlock>();
        hostBlock.Patterns.Length.ShouldBe(2);
        hostBlock.Patterns.ShouldContain("host1");
        hostBlock.Patterns.ShouldContain("host2");
    }

    [Fact]
    public void Parse_MatchBlock_ShouldParseCorrectly()
    {
        var content = "Match host example.com user root\n  Port 22";
        var doc = SshConfigParser.Parse(content);
        doc.Blocks.Length.ShouldBe(1);
        var matchBlock = doc.Blocks[0].ShouldBeOfType<SshMatchBlock>();
        matchBlock.Criteria.Length.ShouldBe(2);
        matchBlock.Criteria[0].Kind.ShouldBe(SshMatchCriterionKind.Host);
        matchBlock.Criteria[0].Pattern.ShouldBe("example.com");
        matchBlock.Criteria[1].Kind.ShouldBe(SshMatchCriterionKind.User);
        matchBlock.Criteria[1].Pattern.ShouldBe("root");
    }

    [Fact]
    public void GetConnectionEntriesFromConfig_ShouldReturnCredentials()
    {
        var content = @"
Host example
    HostName 1.2.3.4
    User alice
    Port 2222

Host key-host
    HostName 5.6.7.8
    IdentityFile ~/.ssh/id_rsa
";
        var doc = SshConfigParser.Parse(content);
        var credentials = doc.GetConnectionEntriesFromConfig().ToList();
        
        credentials.Count.ShouldBe(2);
        
        var example = credentials.First(c => c.Hostname.Contains("1.2.3.4"));
        example.Username.ShouldBe("alice");
        example.Port.ShouldBe(2222);
        example.AuthType.ShouldBe(AuthType.Password);
        
        var keyHost = credentials.First(c => c.Hostname == "5.6.7.8");
        keyHost.AuthType.ShouldBe(AuthType.Key);
    }
    [Fact]
    public void GetConnectionEntriesFromConfig_WithPersonalConfig_ShouldReturnCredentials()
    {
        var content = GetEmbeddedResource("ssh_config_personal");
        var doc = SshConfigParser.Parse(content);
        var credentials = doc.GetConnectionEntriesFromConfig().ToList();
        
        credentials.ShouldNotBeEmpty();
        
        var prodWeb = credentials.FirstOrDefault(c => c.Hostname == "10.10.1.11");
        prodWeb.ShouldNotBeNull();
        
        var keyHost = credentials.FirstOrDefault(c => c.AuthType == AuthType.Key);
        keyHost.ShouldNotBeNull();
    }
}