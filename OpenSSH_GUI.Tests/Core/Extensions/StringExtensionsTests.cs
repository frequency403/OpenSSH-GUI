using OpenSSH_GUI.Core.Extensions;
using Shouldly;
using Xunit;

namespace OpenSSH_GUI.Tests.Core.Extensions;

public class StringExtensionsTests
{
    [Theory]
    [InlineData("HelloWorld", "hello_world")]
    public void ToSnakeCase_Tests(string input, string expected)
    {
        input.ToSnakeCase().ShouldBe(expected);
    }

    [Theory]
    [InlineData("HelloWorld", "helloWorld")]
    public void ToCamelCase_Tests(string input, string expected)
    {
        input.ToCamelCase().ShouldBe(expected);
    }

    [Theory]
    [InlineData("HelloWorld", "hello-world")]
    public void ToKebabCase_Tests(string input, string expected)
    {
        input.ToKebabCase().ShouldBe(expected);
    }

    [Theory]
    [InlineData("hello world", "HelloWorld")]
    public void ToPascalCase_Tests(string input, string expected)
    {
        input.ToPascalCase().ShouldBe(expected);
    }

    [Fact]
    public void SplitToChunks_Tests()
    {
        "abcdef".SplitToChunks(2).ShouldBe(new[] { "ab", "cd", "ef" });
        "abcde".SplitToChunks(2).ShouldBe(new[] { "ab", "cd", "e" });
    }

    [Fact]
    public void Wrap_Tests()
    {
        var input = "abcdef";
        input.Wrap(2, "|").ShouldBe("ab|cd|ef");
    }

    [Fact]
    public void ToTitleCase_Tests()
    {
        "this is a title".ToTitleCase().ShouldBe("This Is A Title");
    }

    [Fact]
    public void ToSentenceCase_Tests()
    {
        "THIS IS A SENTENCE.".ToSentenceCase().ShouldBe("This is a sentence.");
    }

    [Fact]
    public void ToLeetSpeak_Tests()
    {
        "leetspeak".ToLeetSpeak().ShouldBe("l33t5p34k");
    }

    [Fact]
    public void ToStudlyCaps_Tests()
    {
        // Random, so we just check it doesn't throw and length is same
        "studly".ToStudlyCaps().Length.ShouldBe(6);
    }
}