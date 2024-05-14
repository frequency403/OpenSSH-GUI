#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 04.05.2024 - 22:05:48
// Last edit: 14.05.2024 - 03:05:21

#endregion

using System.Text.RegularExpressions;

namespace OpenSSH_GUI.Core.Extensions;

public static partial class StringExtensions
{
    [GeneratedRegex("\\.")]
    private static partial Regex EcapeRegex();

    public static string Wrap(this string input, int maxLength, char? wrapper = null)
    {
        return input.Wrap(maxLength, wrapper is null ? null : wrapper.ToString());
    }

    public static string Wrap(this string input, int maxLength, string? wrapper = null)
    {
        return string.Join(wrapper ?? Environment.NewLine, EcapeRegex().Replace(input, "").SplitToChunks(maxLength));
    }

    public static IEnumerable<string> SplitToChunks(this string input, int chunkSize)
    {
        for (var i = 0; i < input.Length; i += chunkSize)
            yield return input.Substring(i, Math.Min(chunkSize, input.Length - i));
    }
}