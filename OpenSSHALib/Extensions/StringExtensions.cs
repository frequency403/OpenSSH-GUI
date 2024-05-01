using System.Text;

namespace OpenSSHALib.Extensions;

public static class StringExtensions
{
    public static string Wrap(this string input, int maxLength)
    {
        var builder = new StringBuilder();
        var currentPosition = 0;
        while (currentPosition + maxLength < input.Length)
        {
            builder.Append(input.AsSpan(currentPosition, maxLength));
            builder.Append('\n');
            currentPosition += maxLength;
        }
        builder.Append(input.AsSpan(currentPosition));
        return builder.ToString();
    }
}