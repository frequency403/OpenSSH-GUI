namespace OpenSSH_GUI.SshConfig;

/// <summary>
///     Implements the SSH pattern-matching algorithm used for <c>Host</c> directives and
///     certain <c>Match</c> criteria, as specified by <c>ssh_config(5)</c>.
/// </summary>
/// <remarks>
///     Supported pattern syntax:
///     <list type="bullet">
///         <item>
///             <description><c>*</c> — matches any sequence of characters (including none).</description>
///         </item>
///         <item>
///             <description><c>?</c> — matches exactly one character.</description>
///         </item>
///         <item>
///             <description><c>!</c> prefix — negates the pattern; a negated match always takes priority.</description>
///         </item>
///     </list>
/// </remarks>
public static class SshWildcardMatcher
{
    /// <summary>
    ///     Determines whether <paramref name="hostname" /> matches the supplied list of
    ///     patterns according to the OpenSSH evaluation rules.
    /// </summary>
    /// <remarks>
    ///     Evaluation proceeds left-to-right.  A hostname is considered a match when:
    ///     <list type="number">
    ///         <item>
    ///             <description>At least one non-negated pattern matches, <b>and</b></description>
    ///         </item>
    ///         <item>
    ///             <description>No negated (<c>!</c>-prefixed) pattern matches.</description>
    ///         </item>
    ///     </list>
    ///     A negated match always wins regardless of other patterns.
    ///     An empty pattern list never matches.
    /// </remarks>
    /// <param name="hostname">The hostname to test.</param>
    /// <param name="patterns">The ordered list of patterns from the <c>Host</c> line.</param>
    public static bool Matches(ReadOnlySpan<char> hostname, IReadOnlyList<string> patterns)
    {
        var positiveMatch = false;

        foreach (var raw in patterns)
        {
            if (raw.Length == 0)
                continue;

            if (raw[0] == '!')
            {
                if (MatchesGlob(hostname, raw.AsSpan(1)))
                    return false;
            }
            else
            {
                if (!positiveMatch && MatchesGlob(hostname, raw.AsSpan()))
                    positiveMatch = true;
            }
        }

        return positiveMatch;
    }

    /// <summary>
    ///     Tests whether <paramref name="input" /> matches a single SSH glob <paramref name="pattern" />
    ///     containing <c>*</c> and <c>?</c> wildcards.
    ///     Comparison is case-insensitive, consistent with OpenSSH hostname handling.
    /// </summary>
    /// <param name="input">The string to test.</param>
    /// <param name="pattern">The glob pattern (no <c>!</c> prefix expected here).</param>
    public static bool MatchesGlob(ReadOnlySpan<char> input, ReadOnlySpan<char> pattern)
    {
        while (!pattern.IsEmpty)
        {
            var ch = pattern[0];
            pattern = pattern[1..];

            if (ch == '*')
            {
                while (!pattern.IsEmpty && pattern[0] == '*')
                    pattern = pattern[1..];

                if (pattern.IsEmpty)
                    return true;

                for (var i = 0; i <= input.Length; i++)
                    if (MatchesGlob(input[i..], pattern))
                        return true;
                return false;
            }

            if (input.IsEmpty)
                return false;

            if (ch != '?' &&
                char.ToUpperInvariant(ch) != char.ToUpperInvariant(input[0]))
                return false;

            input = input[1..];
        }

        return input.IsEmpty;
    }
}