using System.Collections.Immutable;

namespace OpenSSH_GUI.SshConfig;

/// <summary>
///     Base record for a <c>Host</c> or <c>Match</c> block within an SSH configuration file.
///     A block encompasses all directives from its header line to the next block header or end of file.
/// </summary>
public abstract record SshBlock
{
    private protected SshBlock(
        ImmutableArray<SshLineItem> items,
        int lineNumber,
        string rawHeaderText,
        string? headerComment)
    {
        Items = items;
        LineNumber = lineNumber;
        RawHeaderText = rawHeaderText;
        HeaderComment = headerComment;
    }

    /// <summary>
    ///     Gets the configuration items (directives, comments, blank lines) contained within this block,
    ///     in document order.
    /// </summary>
    public ImmutableArray<SshLineItem> Items { get; init; }

    /// <summary>
    ///     Gets the 1-based line number of the block's header line in the source file, or <c>0</c> for programmatically
    ///     created blocks.
    /// </summary>
    public int LineNumber { get; init; }

    /// <summary>
    ///     Gets the original raw text of the header line (without line terminator),
    ///     used for lossless round-trip serialization.
    /// </summary>
    public string RawHeaderText { get; init; }

    /// <summary>
    ///     Gets the trailing inline comment from the header line,
    ///     including the leading <c>#</c>, or <see langword="null" /> if none was present.
    /// </summary>
    public string? HeaderComment { get; init; }

    /// <summary>
    ///     Returns all <see cref="SshConfigEntry" /> items within this block,
    ///     optionally filtered to a specific keyword.
    /// </summary>
    /// <param name="key">
    ///     Case-insensitive keyword to filter on,
    ///     or <see langword="null" /> to return all entries.
    /// </param>
    public IEnumerable<SshConfigEntry> GetEntries(string? key = null)
    {
        return Items
            .OfType<SshConfigEntry>()
            .Where(e => key is null || e.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    ///     Returns the first <see cref="SshConfigEntry" /> whose key matches <paramref name="key" />,
    ///     or <see langword="null" /> if none exists.
    /// </summary>
    public SshConfigEntry? GetEntry(string key)
    {
        return Items
            .OfType<SshConfigEntry>()
            .FirstOrDefault(e => e.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
///     Represents a <c>Host</c> block whose directives apply to connections whose target hostname
///     matches at least one of the declared patterns (and no negated patterns).
/// </summary>
public sealed record SshHostBlock : SshBlock
{
    /// <param name="patterns">Hostname patterns from the <c>Host</c> line.</param>
    /// <param name="items">Block contents.</param>
    /// <param name="lineNumber">1-based header line number.</param>
    /// <param name="rawHeaderText">Original header line text.</param>
    /// <param name="headerComment">Optional trailing comment from the header.</param>
    public SshHostBlock(
        ImmutableArray<string> patterns,
        ImmutableArray<SshLineItem> items,
        int lineNumber,
        string rawHeaderText,
        string? headerComment)
        : base(items, lineNumber, rawHeaderText, headerComment)
    {
        Patterns = patterns;
    }

    /// <summary>
    ///     Gets the hostname patterns declared on the <c>Host</c> header line.
    ///     <para>
    ///         Patterns may contain <c>*</c> (any sequence) and <c>?</c> (any single character) wildcards,
    ///         and may be prefixed with <c>!</c> to negate the match.
    ///         <c>Host *</c> matches all connections.
    ///     </para>
    /// </summary>
    public ImmutableArray<string> Patterns { get; init; }

    /// <summary>
    ///     Creates a new <see cref="SshHostBlock" /> with no source location,
    ///     suitable for programmatic document construction.
    /// </summary>
    /// <param name="patterns">One or more hostname patterns.</param>
    /// <param name="items">Optional initial block contents.</param>
    public static SshHostBlock Create(IEnumerable<string> patterns, IEnumerable<SshLineItem>? items = null)
    {
        return new SshHostBlock([..patterns], [..(items ?? [])], 0, string.Empty, null);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"Host {string.Join(' ', Patterns)}";
    }
}

/// <summary>
///     Represents a <c>Match</c> block whose directives apply only when all specified criteria are satisfied.
/// </summary>
public sealed record SshMatchBlock : SshBlock
{
    /// <param name="criteria">Match criteria from the <c>Match</c> header line.</param>
    /// <param name="items">Block contents.</param>
    /// <param name="lineNumber">1-based header line number.</param>
    /// <param name="rawHeaderText">Original header line text.</param>
    /// <param name="headerComment">Optional trailing comment from the header.</param>
    public SshMatchBlock(
        ImmutableArray<SshMatchCriterion> criteria,
        ImmutableArray<SshLineItem> items,
        int lineNumber,
        string rawHeaderText,
        string? headerComment)
        : base(items, lineNumber, rawHeaderText, headerComment)
    {
        Criteria = criteria;
    }

    /// <summary>
    ///     Gets the criteria that must all be satisfied simultaneously for this block to apply.
    ///     Evaluated in document order; the first non-matching criterion short-circuits evaluation.
    /// </summary>
    public ImmutableArray<SshMatchCriterion> Criteria { get; init; }

    /// <summary>
    ///     Creates a new <see cref="SshMatchBlock" /> with no source location,
    ///     suitable for programmatic document construction.
    /// </summary>
    /// <param name="criteria">One or more match criteria.</param>
    /// <param name="items">Optional initial block contents.</param>
    public static SshMatchBlock Create(IEnumerable<SshMatchCriterion> criteria, IEnumerable<SshLineItem>? items = null)
    {
        return new SshMatchBlock([..criteria], [..(items ?? [])], 0, string.Empty, null);
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"Match {string.Join(' ', Criteria.Select(static c => c.ToString()))}";
    }
}