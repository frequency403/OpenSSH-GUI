#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:25

#endregion

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace OpenSSH_GUI.Core.Extensions;
/// <summary>
/// Provides extension methods for string manipulation.
/// </summary>
public static partial class StringExtensions
{
    private static readonly RandomNumberGenerator _generator = RandomNumberGenerator.Create();

    public static string? Encrypt(string? input)
    {
        if (input is null) return input;
        var prependBytes = new byte[3];
        var appendBytes = new byte[3];
        _generator.GetNonZeroBytes(prependBytes);
        _generator.GetNonZeroBytes(appendBytes);
        var bytes = prependBytes.ToList();
        bytes.AddRange(Encoding.UTF8.GetBytes(input));
        bytes.AddRange(appendBytes);
        bytes.Reverse();
        return Convert.ToBase64String(bytes.ToArray());
    }

    public static string? Decrypt(string? input)
    {
        if (input is null) return input;
        var rearrangedBytes = Convert.FromBase64String(input).Reverse().ToList();
        rearrangedBytes.RemoveRange(0, 3);
        rearrangedBytes.RemoveRange(rearrangedBytes.Count - 3, 3);
        return Encoding.UTF8.GetString(rearrangedBytes.ToArray());
    }
    
    
    [GeneratedRegex("\\.")]
    private static partial Regex EcapeRegex();

     /// <summary>
    /// Wraps the input string to the specified maximum length, optionally enclosing each chunk in a specified character.
    /// </summary>
    /// <param name="input">The input string to wrap.</param>
    /// <param name="maxLength">The maximum length of each wrapped chunk.</param>
    /// <param name="wrapper">Optional. The character to use as a wrapper around each chunk. If null, uses a new line character.</param>
    /// <returns>The wrapped string.</returns>
    /// <example>
    /// <code>
    /// string wrapped = "This is a long string that needs wrapping.".Wrap(10, '-');
    /// Console.WriteLine(wrapped);
    /// </code>
    /// <code>
    /// // Output:
    /// // This is a- 
    /// // long stri-
    /// // ng that n-
    /// // eeds wra-
    /// // pping.
    /// </code>
    /// </example>
    public static string Wrap(this string input, int maxLength, char? wrapper = null)
    {
        return input.Wrap(maxLength, wrapper is null ? null : wrapper.ToString());
    }

    /// <summary>
    /// Wraps the input string to the specified maximum length, optionally enclosing each chunk in a specified string.
    /// </summary>
    /// <param name="input">The input string to wrap.</param>
    /// <param name="maxLength">The maximum length of each wrapped chunk.</param>
    /// <param name="wrapper">Optional. The string to use as a wrapper around each chunk. If null, uses a new line.</param>
    /// <returns>The wrapped string.</returns>
    /// <example>
    /// <code>
    /// string wrapped = "This is a long string that needs wrapping.".Wrap(10, " | ");
    /// Console.WriteLine(wrapped);
    /// </code>
    /// <code>
    /// // Output:
    /// // This is a | long stri | ng that n | eeds wra | pping.
    /// </code>
    /// </example>
    public static string Wrap(this string input, int maxLength, string? wrapper = null)
    {
        return string.Join(wrapper ?? Environment.NewLine, EcapeRegex().Replace(input, "").SplitToChunks(maxLength));
    }

    /// <summary>
    /// Splits the input string into chunks of the specified size.
    /// </summary>
    /// <param name="input">The input string to split.</param>
    /// <param name="chunkSize">The size of each chunk.</param>
    /// <returns>An enumerable collection of chunked strings.</returns>
    /// <example>
    /// <code>
    /// var chunks = "This is a long string".SplitToChunks(5);
    /// foreach (string chunk in chunks)
    /// {
    ///     Console.WriteLine(chunk);
    /// }
    /// </code>
    /// <code>
    /// // Output:
    /// // This 
    /// // is a 
    /// // long 
    /// // strin
    /// // g
    /// </code>
    /// </example>
    public static IEnumerable<string> SplitToChunks(this string input, int chunkSize)
    {
        for (var i = 0; i < input.Length; i += chunkSize)
            yield return input.Substring(i, Math.Min(chunkSize, input.Length - i));
    }
    
    /// <summary>
    /// Converts the given string to snake_case.
    /// </summary>
    /// <param name="str">The input string to convert.</param>
    /// <returns>The string converted to snake_case.</returns>
    /// <example>
    /// <code>
    /// string snakeCase = "PascalCaseString".ToSnakeCase();
    /// Console.WriteLine(snakeCase);
    /// </code>
    /// <code>
    /// // Output:
    /// // pascal_case_string
    /// </code>
    /// </example>
    public static string ToSnakeCase(this string str)
    {
        return Regex.Replace(str, "(?<!^)([A-Z][a-z]|(?<=[a-z])[A-Z])", "_$1").ToLower();
    }

        /// <summary>
    /// Converts the given string to camelCase.
    /// </summary>
    /// <param name="str">The input string to convert.</param>
    /// <returns>The string converted to camelCase.</returns>
    /// <example>
    /// <code>
    /// string camelCase = "PascalCaseString".ToCamelCase();
    /// Console.WriteLine(camelCase);
    /// </code>
    /// <code>
    /// // Output:
    /// // pascalCaseString
    /// </code>
    /// </example>
    public static string ToCamelCase(this string str)
    {
        if (string.IsNullOrEmpty(str)) return str;
        return char.ToLowerInvariant(str[0]) + str.Substring(1);
    }

    /// <summary>
    /// Converts the given string to kebab-case.
    /// </summary>
    /// <param name="str">The input string to convert.</param>
    /// <returns>The string converted to kebab-case.</returns>
    /// <example>
    /// <code>
    /// string kebabCase = "PascalCaseString".ToKebabCase();
    /// Console.WriteLine(kebabCase);
    /// </code>
    /// <code>
    /// // Output:
    /// // pascal-case-string
    /// </code>
    /// </example>
    public static string ToKebabCase(this string str)
    {
        return Regex.Replace(str, "(?<!^)([A-Z][a-z]|(?<=[a-z])[A-Z])", "-$1").ToLower();
    }

    /// <summary>
    /// Converts the given string to PascalCase.
    /// </summary>
    /// <param name="str">The input string to convert.</param>
    /// <returns>The string converted to PascalCase.</returns>
    /// <example>
    /// <code>
    /// string pascalCase = "snake_case_string".ToPascalCase();
    /// Console.WriteLine(pascalCase);
    /// </code>
    /// <code>
    /// // Output:
    /// // SnakeCaseString
    /// </code>
    /// </example>
    public static string ToPascalCase(this string str)
    {
        return Regex.Replace(str, @"(^\w)|(\s\w)", m => m.Value.ToUpper()).Replace(" ", "");
    }

    /// <summary>
    /// Converts the given string to Title Case.
    /// </summary>
    /// <param name="str">The input string to convert.</param>
    /// <returns>The string converted to Title Case.</returns>
    /// <example>
    /// <code>
    /// string titleCase = "this is a title case string".ToTitleCase();
    /// Console.WriteLine(titleCase);
    /// </code>
    /// <code>
    /// // Output:
    /// // This Is A Title Case String
    /// </code>
    /// </example>
    public static string ToTitleCase(this string str)
    {
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(str.ToLower());
    }

    /// <summary>
    /// Converts the given string to Sentence case.
    /// </summary>
    /// <param name="str">The input string to convert.</param>
    /// <returns>The string converted to Sentence case.</returns>
    /// <example>
    /// <code>
    /// string sentenceCase = "THIS IS A SENTENCE CASE STRING.".ToSentenceCase();
    /// Console.WriteLine(sentenceCase);
    /// </code>
    /// <code>
    /// // Output:
    /// // This is a sentence case string.
    /// </code>
    /// </example>
    public static string ToSentenceCase(this string str)
    {
        if (string.IsNullOrEmpty(str)) return str;
        return char.ToUpperInvariant(str[0]) + str.Substring(1).ToLower();
    }

    /// <summary>
    /// Converts the given string to StUdLyCaPs.
    /// </summary>
    /// <param name="str">The input string to convert.</param>
    /// <returns>The string converted to StUdLyCaPs.</returns>
    /// <example>
    /// <code>
    /// string studlyCaps = "this is a studly caps string".ToStudlyCaps();
    /// Console.WriteLine(studlyCaps);
    /// </code>
    /// <code>
    /// // Output:
    /// // tHiS Is a sTuDlY CaPs sTrInG
    /// </code>
    /// </example>
    public static string ToStudlyCaps(this string str)
    {
        var random = new Random();
        return str.Aggregate("", (current, t) => current + (random.Next(2) == 0 ? char.ToUpper(t) : char.ToLower(t)));
    }

    /// <summary>
    /// Converts the given string to Leet Speak.
    /// </summary>
    /// <param name="str">The input string to convert.</param>
    /// <returns>The string converted to Leet Speak.</returns>
    /// <example>
    /// <code>
    /// string leetSpeak = "Leet Speak is cool!".ToLeetSpeak();
    /// Console.WriteLine(leetSpeak);
    /// </code>
    /// <code>
    /// // Output:
    /// // 133t Sp34k 15 c00l!
    /// </code>
    /// </example>
    public static string ToLeetSpeak(this string str)
    {
        return str.Replace('e', '3').Replace('a', '4').Replace('o', '0').Replace('i', '1').Replace('s', '5');
    }
}
