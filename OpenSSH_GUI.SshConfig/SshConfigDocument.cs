using System.Collections.Immutable;

namespace OpenSSH_GUI.SshConfig;

/// <summary>
///     Represents the fully parsed, <c>Include</c>-resolved contents of one or more SSH client
///     configuration files.
/// </summary>
/// <remarks>
///     <para>
///         Items in <see cref="GlobalItems" /> precede the first <c>Host</c> or <c>Match</c> block and
///         apply to every connection unless overridden inside a block.
///     </para>
///     <para>
///         All <c>Include</c> directives are resolved during parsing; the resulting document is flat
///         and contains no references to external files.  Round-trip serialization therefore cannot
///         reproduce the original <c>Include</c> lines.
///     </para>
///     <para>
///         OpenSSH evaluates blocks top-to-bottom and the first match wins; the order of
///         <see cref="Blocks" /> is therefore significant.
///     </para>
/// </remarks>
public sealed record SshConfigDocument
{
    /// <param name="globalItems">Items at global scope.</param>
    /// <param name="blocks">All host and match blocks, in document order.</param>
    public SshConfigDocument(ImmutableArray<SshLineItem> globalItems, ImmutableArray<SshBlock> blocks)
    {
        GlobalItems = globalItems;
        Blocks = blocks;
    }

    /// <summary>
    ///     Gets the items (directives, comments, blank lines) that appear before the first
    ///     <c>Host</c> or <c>Match</c> block.
    /// </summary>
    public ImmutableArray<SshLineItem> GlobalItems { get; init; }

    /// <summary>
    ///     Gets all <c>Host</c> and <c>Match</c> blocks in document order.
    /// </summary>
    public ImmutableArray<SshBlock> Blocks { get; init; }

    /// <summary>An empty document with no items or blocks.</summary>
    public static SshConfigDocument Empty { get; } = new([], []);

    /// <summary>Returns all <see cref="SshHostBlock" /> instances in document order.</summary>
    public IEnumerable<SshHostBlock> HostBlocks =>
        Blocks.OfType<SshHostBlock>();

    /// <summary>Returns all <see cref="SshMatchBlock" /> instances in document order.</summary>
    public IEnumerable<SshMatchBlock> MatchBlocks =>
        Blocks.OfType<SshMatchBlock>();

    /// <summary>
    ///     Returns all <see cref="SshConfigEntry" /> items at global scope,
    ///     optionally filtered to a specific keyword.
    /// </summary>
    /// <param name="key">Case-insensitive keyword to filter on, or <see langword="null" /> for all entries.</param>
    public IEnumerable<SshConfigEntry> GetGlobalEntries(string? key = null)
    {
        return GlobalItems
            .OfType<SshConfigEntry>()
            .Where(e => key is null || e.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    ///     Returns all <see cref="SshHostBlock" /> instances whose <see cref="SshHostBlock.Patterns" />
    ///     match <paramref name="hostname" /> according to the SSH wildcard rules implemented by
    ///     <see cref="SshWildcardMatcher" />.
    /// </summary>
    /// <param name="hostname">The target hostname to test.</param>
    public IEnumerable<SshHostBlock> GetMatchingHostBlocks(string hostname)
    {
        return HostBlocks.Where(b => SshWildcardMatcher.Matches(hostname.AsSpan(), b.Patterns));
    }
}