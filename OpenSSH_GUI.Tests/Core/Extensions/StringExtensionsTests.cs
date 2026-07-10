using Avalonia.Headless.XUnit;
using OpenSSH_GUI.Core.Extensions;
using Shouldly;
using Xunit;

namespace OpenSSH_GUI.Tests.Core.Extensions;

public class StringExtensionsTests
{
    [AvaloniaTheory, InlineData("HelloWorld", "hello_world")]
    public void ToSnakeCase_Tests(string input, string expected) { input.ToSnakeCase().ShouldBe(expected); }

    [AvaloniaTheory, InlineData("HelloWorld", "helloWorld")]
    public void ToCamelCase_Tests(string input, string expected) { input.ToCamelCase().ShouldBe(expected); }

    [AvaloniaTheory, InlineData("HelloWorld", "hello-world")]
    public void ToKebabCase_Tests(string input, string expected) { input.ToKebabCase().ShouldBe(expected); }

    [AvaloniaTheory, InlineData("hello world", "HelloWorld")]
    public void ToPascalCase_Tests(string input, string expected) { input.ToPascalCase().ShouldBe(expected); }

    [AvaloniaFact]
    public void SplitToChunks_Tests()
    {
        "abcdef".SplitToChunks(2).ShouldBe(["ab", "cd", "ef"]);
        "abcde".SplitToChunks(2).ShouldBe(["ab", "cd", "e"]);
    }

    [AvaloniaFact]
    public void Wrap_Tests()
    {
        var input = "abcdef";
        input.Wrap(2, "|").ShouldBe("ab|cd|ef");
    }

    [AvaloniaFact]
    public void ToTitleCase_Tests() { "this is a title".ToTitleCase().ShouldBe("This Is A Title"); }

    [AvaloniaFact]
    public void ToSentenceCase_Tests() { "THIS IS A SENTENCE.".ToSentenceCase().ShouldBe("This is a sentence."); }

    [AvaloniaFact]
    public void ToLeetSpeak_Tests() { "leetspeak".ToLeetSpeak().ShouldBe("l33t5p34k"); }

    [AvaloniaFact]
    public void ToStudlyCaps_Tests()
    {
        // Random, so we just check it doesn't throw and length is same
        "studly".ToStudlyCaps().Length.ShouldBe(6);
    }
}