using OpenSSH_GUI.SshConfig;
using Shouldly;
using Xunit;

namespace OpenSSH_GUI.Tests.SshConfig;

public class SshConfigParserTests
{
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
}
