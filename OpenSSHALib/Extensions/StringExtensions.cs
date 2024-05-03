using System.Text;
using System.Text.RegularExpressions;

namespace OpenSSHALib.Extensions;

public static partial class StringExtensions
{
    [GeneratedRegex("\\.")]
    private static partial Regex EcapeRegex();

    public static string Wrap(this string input, int maxLength, char? wrapper = null) =>
        input.Wrap(maxLength, wrapper is null ? null : wrapper.ToString());
    public static string Wrap(this string input, int maxLength, string? wrapper = null) =>
        string.Join(wrapper ?? Environment.NewLine, EcapeRegex().Replace(input, "").SplitToChunks(maxLength));
    
    public static IEnumerable<string> SplitToChunks(this string input, int chunkSize)
    {
        for (int i = 0; i < input.Length; i += chunkSize)
        {
            yield return input.Substring(i, Math.Min(chunkSize, input.Length - i));
        }
    }
}