using System.Globalization;
using System.Text.RegularExpressions;

namespace OpenSSH_GUI.Core.Extensions;

/// <summary>
///     Provides extension methods for string manipulation.
/// </summary>
public static partial class StringExtensions
{
    [GeneratedRegex("\\.")]
    private static partial Regex EcapeRegex();

    /// <param name="input">The input string to wrap.</param>
    extension(string input)
    {
        /// <summary>
        ///     Resolves a absolute path from a relative path which can contain <c>~</c> or <c>~user</c> or <c>%AppData%</c> or
        ///     <c>%UserProfile%</c> etc.
        /// </summary>
        public string ResolvePath()
        {
            var path = input;
            path = Environment.ExpandEnvironmentVariables(path);
            if (!path.StartsWith('~')) return Path.GetFullPath(path);
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            path = path.Length == 1 ? home : Path.Combine(home, path[2..]);
            return Path.GetFullPath(path);
        }

        /// <summary>
        ///     Wraps the input string to the specified maximum length, optionally enclosing each chunk in a specified character.
        /// </summary>
        /// <param name="maxLength">The maximum length of each wrapped chunk.</param>
        /// <param name="wrapper">
        ///     Optional. The character to use as a wrapper around each chunk. If null, uses a new line
        ///     character.
        /// </param>
        /// <returns>The wrapped string.</returns>
        /// <example>
        ///     <code>
        /// string wrapped = "This is a long string that needs wrapping.".Wrap(10, '-');
        /// Console.WriteLine(wrapped);
        /// </code>
        ///     <code>
        /// // Output:
        /// // This is a- 
        /// // long stri-
        /// // ng that n-
        /// // eeds wra-
        /// // pping.
        /// </code>
        /// </example>
        public string Wrap(int maxLength, char? wrapper = null) => input.Wrap(maxLength, wrapper is null ? null : wrapper.ToString());

        /// <summary>
        ///     Wraps the input string to the specified maximum length, optionally enclosing each chunk in a specified string.
        /// </summary>
        /// <param name="maxLength">The maximum length of each wrapped chunk.</param>
        /// <param name="wrapper">Optional. The string to use as a wrapper around each chunk. If null, uses a new line.</param>
        /// <returns>The wrapped string.</returns>
        /// <example>
        ///     <code>
        /// string wrapped = "This is a long string that needs wrapping.".Wrap(10, " | ");
        /// Console.WriteLine(wrapped);
        /// </code>
        ///     <code>
        /// // Output:
        /// // This is a | long stri | ng that n | eeds wra | pping.
        /// </code>
        /// </example>
        public string Wrap(int maxLength, string? wrapper = null) => string.Join(
            wrapper ?? Environment.NewLine,
            EcapeRegex().Replace(input, string.Empty).SplitToChunks(maxLength));

        /// <summary>
        ///     Splits the input string into chunks of the specified size.
        /// </summary>
        /// <param name="chunkSize">The size of each chunk.</param>
        /// <returns>An enumerable collection of chunked strings.</returns>
        /// <example>
        ///     <code>
        /// var chunks = "This is a long string".SplitToChunks(5);
        /// foreach (string chunk in chunks)
        /// {
        ///     Console.WriteLine(chunk);
        /// }
        /// </code>
        ///     <code>
        /// // Output:
        /// // This 
        /// // is a 
        /// // long 
        /// // strin
        /// // g
        /// </code>
        /// </example>
        public IEnumerable<string> SplitToChunks(int chunkSize)
        {
            for (var i = 0; i < input.Length; i += chunkSize)
                yield return input.Substring(i, Math.Min(chunkSize, input.Length - i));
        }

        /// <summary>
        ///     Converts the given string to snake_case.
        /// </summary>
        /// <returns>The string converted to snake_case.</returns>
        /// <example>
        ///     <code>
        /// string snakeCase = "PascalCaseString".ToSnakeCase();
        /// Console.WriteLine(snakeCase);
        /// </code>
        ///     <code>
        /// // Output:
        /// // pascal_case_string
        /// </code>
        /// </example>
        public string ToSnakeCase() => Regex.Replace(input, "(?<!^)([A-Z][a-z]|(?<=[a-z])[A-Z])", "_$1").ToLower();

        /// <summary>
        ///     Converts the given string to camelCase.
        /// </summary>
        /// <returns>The string converted to camelCase.</returns>
        /// <example>
        ///     <code>
        /// string camelCase = "PascalCaseString".ToCamelCase();
        /// Console.WriteLine(camelCase);
        /// </code>
        ///     <code>
        /// // Output:
        /// // pascalCaseString
        /// </code>
        /// </example>
        public string ToCamelCase()
        {
            if (string.IsNullOrEmpty(input)) return input;
            return char.ToLowerInvariant(input[0]) + input.Substring(1);
        }

        /// <summary>
        ///     Converts the given string to kebab-case.
        /// </summary>
        /// <returns>The string converted to kebab-case.</returns>
        /// <example>
        ///     <code>
        /// string kebabCase = "PascalCaseString".ToKebabCase();
        /// Console.WriteLine(kebabCase);
        /// </code>
        ///     <code>
        /// // Output:
        /// // pascal-case-string
        /// </code>
        /// </example>
        public string ToKebabCase() => Regex.Replace(input, "(?<!^)([A-Z][a-z]|(?<=[a-z])[A-Z])", "-$1").ToLower();

        /// <summary>
        ///     Converts the given string to PascalCase.
        /// </summary>
        /// <returns>The string converted to PascalCase.</returns>
        /// <example>
        ///     <code>
        /// string pascalCase = "snake_case_string".ToPascalCase();
        /// Console.WriteLine(pascalCase);
        /// </code>
        ///     <code>
        /// // Output:
        /// // SnakeCaseString
        /// </code>
        /// </example>
        public string ToPascalCase() { return Regex.Replace(input, @"(^\w)|(\s\w)", m => m.Value.ToUpper()).Replace(" ", string.Empty); }

        /// <summary>
        ///     Converts the given string to Title Case.
        /// </summary>
        /// <returns>The string converted to Title Case.</returns>
        /// <example>
        ///     <code>
        /// string titleCase = "this is a title case string".ToTitleCase();
        /// Console.WriteLine(titleCase);
        /// </code>
        ///     <code>
        /// // Output:
        /// // This Is A Title Case String
        /// </code>
        /// </example>
        public string ToTitleCase() => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(input.ToLower());

        /// <summary>
        ///     Converts the given string to Sentence case.
        /// </summary>
        /// <returns>The string converted to Sentence case.</returns>
        /// <example>
        ///     <code>
        /// string sentenceCase = "THIS IS A SENTENCE CASE STRING.".ToSentenceCase();
        /// Console.WriteLine(sentenceCase);
        /// </code>
        ///     <code>
        /// // Output:
        /// // This is a sentence case string.
        /// </code>
        /// </example>
        public string ToSentenceCase()
        {
            if (string.IsNullOrEmpty(input)) return input;
            return char.ToUpperInvariant(input[0]) + input.Substring(1).ToLower();
        }

        /// <summary>
        ///     Converts the given string to StUdLyCaPs.
        /// </summary>
        /// <returns>The string converted to StUdLyCaPs.</returns>
        /// <example>
        ///     <code>
        /// string studlyCaps = "this is a studly caps string".ToStudlyCaps();
        /// Console.WriteLine(studlyCaps);
        /// </code>
        ///     <code>
        /// // Output:
        /// // tHiS Is a sTuDlY CaPs sTrInG
        /// </code>
        /// </example>
        public string ToStudlyCaps()
        {
            var random = new Random();
            return input.Aggregate(
                string.Empty,
                (current, t) => current + (random.Next(2) == 0 ? char.ToUpper(t) : char.ToLower(t)));
        }

        /// <summary>
        ///     Converts the given string to Leet Speak.
        /// </summary>
        /// <returns>The string converted to Leet Speak.</returns>
        /// <example>
        ///     <code>
        /// string leetSpeak = "Leet Speak is cool!".ToLeetSpeak();
        /// Console.WriteLine(leetSpeak);
        /// </code>
        ///     <code>
        /// // Output:
        /// // 133t Sp34k 15 c00l!
        /// </code>
        /// </example>
        public string ToLeetSpeak() => input.Replace('e', '3').Replace('a', '4').Replace('o', '0').Replace('i', '1').Replace('s', '5');
    }
}