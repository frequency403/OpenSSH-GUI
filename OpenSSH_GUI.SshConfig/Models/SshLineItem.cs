using System.Collections.Immutable;

namespace OpenSSH_GUI.SshConfig.Models;

/// <summary>
///     Base record for any source-level item that can appear in an SSH configuration —
///     either at global scope or within a <see cref="SshHostBlock" /> or <see cref="SshMatchBlock" />.
/// </summary>
public abstract record SshLineItem
{
    private protected SshLineItem(int lineNumber, string rawText)
    {
        LineNumber = lineNumber;
        RawText = rawText;
    }

    /// <summary>Gets the 1-based line number in the originating source file, or <c>0</c> for programmatically created items.</summary>
    public int LineNumber { get; init; }

    /// <summary>
    ///     Gets the original, unmodified text of the line (without the line terminator).
    ///     <para>
    ///         When this is <see cref="string.Empty" />, the serializer regenerates the line from structured data
    ///         regardless of the current <c>RoundTrip</c> setting.
    ///         To force regeneration of a modified entry during round-trip serialization,
    ///         use <see langword="with" /> <c>{ RawText = string.Empty }</c>.
    ///     </para>
    /// </summary>
    public string RawText { get; init; }
}

/// <summary>
///     Represents a blank line, preserved so that round-trip serialization can reproduce
///     the original whitespace layout of the configuration file.
/// </summary>
public sealed record SshBlankLine : SshLineItem
{
    /// <param name="lineNumber">1-based source line number.</param>
    public SshBlankLine(int lineNumber) : base(lineNumber, string.Empty) { }

    /// <summary>Creates a blank line not associated with any source position.</summary>
    public static SshBlankLine Create() => new(0);
}

/// <summary>
///     Represents a standalone comment line whose first non-whitespace character is <c>#</c>.
/// </summary>
public sealed record SshCommentLine : SshLineItem
{
    /// <param name="comment">Comment text (must begin with <c>#</c>).</param>
    /// <param name="lineNumber">1-based source line number.</param>
    /// <param name="rawText">Original line text.</param>
    public SshCommentLine(string comment, int lineNumber, string rawText)
        : base(lineNumber, rawText) => Comment = comment;

    /// <summary>
    ///     Gets the full comment text, including the leading <c>#</c> character
    ///     and any original indentation that was part of the raw line.
    /// </summary>
    public string Comment { get; }

    /// <summary>
    ///     Creates a new <see cref="SshCommentLine" /> not associated with any source position,
    ///     suitable for programmatic document construction.
    /// </summary>
    /// <param name="text">Comment text. A leading <c># </c> is prepended if absent.</param>
    public static SshCommentLine Create(string text)
    {
        var comment = text.StartsWith('#') ? text : $"# {text}";
        return new SshCommentLine(comment, 0, string.Empty);
    }
}

/// <summary>
///     Represents a key-value configuration directive within an SSH configuration file.
/// </summary>
/// <remarks>
///     <para>
///         The <see cref="Key" /> is always stored in canonical casing as defined by
///         <see cref="SshKnownKeys.Normalize" />; unrecognised keywords are stored verbatim.
///     </para>
///     <para>
///         <see cref="Values" /> contains the individual parsed value tokens from one directive line.
///         For keys that accumulate across multiple occurrences (e.g. <c>IdentityFile</c>),
///         each occurrence is a separate <see cref="SshConfigEntry" /> in the parent block.
///         For keys that accept multiple space-separated tokens on a single line
///         (e.g. <c>SendEnv LANG LC_*</c>), all tokens appear in <see cref="Values" />.
///     </para>
/// </remarks>
public sealed record SshConfigEntry : SshLineItem
{
    /// <param name="key">Canonical keyword.</param>
    /// <param name="values">Parsed value tokens.</param>
    /// <param name="inlineComment">Optional trailing comment (including <c>#</c>).</param>
    /// <param name="lineNumber">1-based source line number.</param>
    /// <param name="rawText">Original line text.</param>
    public SshConfigEntry(
        string key,
        ImmutableArray<string> values,
        string? inlineComment,
        int lineNumber,
        string rawText)
        : base(lineNumber, rawText)
    {
        Key = key;
        Values = values;
        InlineComment = inlineComment;
    }

    /// <summary>Gets the configuration keyword in canonical casing, e.g. <c>IdentityFile</c>.</summary>
    public string Key { get; }

    /// <summary>
    ///     Gets the parsed value tokens for this directive line.
    ///     Most directives yield exactly one token; multi-token directives (e.g. <c>SendEnv</c>)
    ///     yield more than one.
    /// </summary>
    public ImmutableArray<string> Values { get; init; }

    /// <summary>
    ///     Gets the trailing inline comment that followed the value on the same source line,
    ///     including the leading <c>#</c>, or <see langword="null" /> if none was present.
    /// </summary>
    public string? InlineComment { get; }

    /// <summary>
    ///     Gets the first (and typically only) value token,
    ///     or <see langword="null" /> if <see cref="Values" /> is empty.
    /// </summary>
    public string? Value => Values.IsDefaultOrEmpty ? null : Values[0];

    /// <summary>
    ///     Creates a new <see cref="SshConfigEntry" /> with no source location,
    ///     suitable for programmatic document construction.
    ///     The key is automatically normalised via <see cref="SshKnownKeys.Normalize" />.
    /// </summary>
    /// <param name="key">Configuration keyword (case-insensitive).</param>
    /// <param name="values">One or more value tokens.</param>
    public static SshConfigEntry Create(string key, params string[] values) => new(SshKnownKeys.Normalize(key), [..values], null, 0, string.Empty);
}