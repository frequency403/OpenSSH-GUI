using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.SshConfig.Exceptions;
using OpenSSH_GUI.SshConfig.Extensions;
using OpenSSH_GUI.SshConfig.Models;
using OpenSSH_GUI.SshConfig.Options;
using OpenSSH_GUI.SshConfig.Parsers;
using OpenSSH_GUI.SshConfig.Services;
using Shouldly;
using Xunit;

namespace OpenSSH_GUI.Tests.SshConfig;

public class SshConfigParserTests
{
    private IFileProvider GetEmbeddedFileProvider()
    {
        return new EmbeddedFileProvider(typeof(SshConfigParserTests).Assembly, "OpenSSH_GUI.Tests.Assets.Testfiles");
    }
    
    private string GetEmbeddedResource(string fileName)
    {
        var assembly = typeof(SshConfigParserTests).Assembly;
        var resourceName = $"OpenSSH_GUI.Tests.Assets.Testfiles.{fileName}";
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            throw new Exception(
                $"Resource {resourceName} not found. Available: {string.Join(", ", assembly.GetManifestResourceNames())}");
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
    public void Parse_PersonalConfig_Into_Config_DependencyInjection()
    {
        var sc = new ConfigurationBuilder();
        sc.AddSshConfig(GetEmbeddedFileProvider(), "ssh_config_personal", false, true);

        var configurationRoot = sc.Build();

        var ss = configurationRoot.GetSection("SshConfig").Get<SshConfiguration>();
        Assert.NotNull(ss);
            
        var ifsCount = ss.Hosts.Where(host => host.IdentityFiles is not null).Sum(host => host.IdentityFiles?.Length);
        ifsCount.ShouldNotBe(null);
        if(ifsCount is {  } count)
            count.ShouldBeGreaterThan(0);
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
        var userDeploy = matchBlocks.FirstOrDefault(b =>
            b.Criteria.Any(c => c is { Kind: SshMatchCriterionKind.User, Pattern: "deploy" }));
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

    [Fact]
    public void Parse_IncludeRecursion_ShouldThrow()
    {
        // Arrange
        var content = "Include recursive.conf";
        var options = new SshConfigParserOptions
            { MaxIncludeDepth = 1, IncludeBasePath = Directory.GetCurrentDirectory() };
        var recursiveFile = Path.Combine(Directory.GetCurrentDirectory(), "recursive.conf");
        File.WriteAllText(recursiveFile, "Include recursive.conf");

        try
        {
            // Act & Assert
            Assert.Throws<SshConfigParseException>(() => SshConfigParser.Parse(content, options));
        }
        finally
        {
            if (File.Exists(recursiveFile)) File.Delete(recursiveFile);
        }
    }

    [Fact]
    public void Parse_UnknownKey_WithStrictOptions_ShouldThrow()
    {
        // Arrange
        var content = "UnknownKey value";
        var options = SshConfigParserOptions.Strict;

        // Act & Assert
        Assert.Throws<SshConfigParseException>(() => SshConfigParser.Parse(content, options));
    }

    [Fact]
    public void Parse_InvalidPort_ShouldBeHandledInSettings()
    {
        // Arrange
        var content = "Host myserver\n  Port invalid";

        // Act
        var doc = SshConfigParser.Parse(content);
        var block = doc.HostBlocks.First();
        var settings = block.GetSettings();

        // Assert
        Assert.Null(settings.Port);
        // Note: In SshHostBlockExtensions.GetSettings, unparseable "Port" is added to otherEntries
        Assert.Single(settings.OtherEntries);
        Assert.Equal("Port", settings.OtherEntries[0].Key);
    }

    [Fact]
    public void Parse_QuotedValues_ShouldStripDoubleQuotes()
    {
        // Arrange
        var content = "Host \"quoted server\"\n  User alice\n  IdentityFile \"~/.ssh/id rsa\"";

        // Act
        var doc = SshConfigParser.Parse(content);
        var block = doc.HostBlocks.First();
        var settings = block.GetSettings();

        // Assert
        Assert.Equal("quoted server", block.Patterns[0]);
        Assert.Equal("alice", settings.User);
        Assert.Contains("~/.ssh/id rsa", settings.IdentityFiles);
    }

    [Fact]
    public void Parse_EmptyLinesAndComments_ShouldPreserve()
    {
        // Arrange
        var content = "\n# Top comment\nHost myserver\n\n  # Entry comment\n  User alice\n";

        // Act
        var doc = SshConfigParser.Parse(content);

        // Assert
        Assert.Equal(2, doc.GlobalItems.Length);
        Assert.IsType<SshBlankLine>(doc.GlobalItems[0]);
        Assert.IsType<SshCommentLine>(doc.GlobalItems[1]);

        var block = doc.HostBlocks.First();
        // Items are: BlankLine, CommentLine, ConfigEntry (User alice), BlankLine (from the \n at the end)
        Assert.Equal(4, block.Items.Length);
        Assert.IsType<SshBlankLine>(block.Items[0]);
        Assert.IsType<SshCommentLine>(block.Items[1]);
        Assert.IsType<SshConfigEntry>(block.Items[2]);
        Assert.IsType<SshBlankLine>(block.Items[3]);
    }

    [Fact]
    public void Parse_MatchCriteria_AllSupported()
    {
        // Arrange
        var content = "Match host h user u port 22 localuser lu address a";

        // Act
        var doc = SshConfigParser.Parse(content);
        var block = (SshMatchBlock)doc.Blocks.First();

        // Assert
        Assert.Equal(5, block.Criteria.Length);
        Assert.Contains(block.Criteria, c => c.Kind == SshMatchCriterionKind.Host);
        Assert.Contains(block.Criteria, c => c.Kind == SshMatchCriterionKind.User);
        Assert.Contains(block.Criteria, c => c.Kind == SshMatchCriterionKind.Port);
        Assert.Contains(block.Criteria, c => c.Kind == SshMatchCriterionKind.LocalUser);
        Assert.Contains(block.Criteria, c => c.Kind == SshMatchCriterionKind.Address);
    }

    [Fact]
    public void Parse_MatchCriteria_Invalid_ShouldThrow()
    {
        // Arrange
        var content = "Match unknown criteria";

        // Act & Assert
        Assert.Throws<SshConfigParseException>(() => SshConfigParser.Parse(content));
    }
}