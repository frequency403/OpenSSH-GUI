namespace OpenSSH_GUI.SshConfig;

/// <summary>
///     Controls how <see cref="SshConfigSerializer" /> formats the output when serializing a
///     <see cref="SshConfigDocument" />.
/// </summary>
public sealed record SshSerializerOptions
{
    /// <summary>
    ///     Gets a value indicating whether items with a non-empty <see cref="SshLineItem.RawText" />
    ///     are written verbatim, preserving the original formatting, casing, and inline comments.
    ///     <para>
    ///         When <see langword="true" />, only items whose <see cref="SshLineItem.RawText" /> is
    ///         <see cref="string.Empty" /> are regenerated from structured data.
    ///         This allows selective modification: set <c>RawText = string.Empty</c> on any entry
    ///         you want the serializer to reformat.
    ///     </para>
    ///     <para>
    ///         When <see langword="false" /> (the default), all output is regenerated using
    ///         <see cref="Indent" />, <see cref="KeyValueSeparator" />, and <see cref="NewLine" />.
    ///     </para>
    /// </summary>
    public bool RoundTrip { get; init; }

    /// <summary>
    ///     Gets the string used to indent directives inside <c>Host</c> and <c>Match</c> blocks.
    ///     Defaults to four spaces.
    /// </summary>
    public string Indent { get; init; } = "    ";

    /// <summary>
    ///     Gets the separator inserted between a keyword and its value in clean-format output.
    ///     Use <c>" "</c> (the default) for <c>Key Value</c> style,
    ///     or <c>"="</c> for <c>Key=Value</c>, or <c>" = "</c> for <c>Key = Value</c>.
    /// </summary>
    public string KeyValueSeparator { get; init; } = " ";

    /// <summary>
    ///     Gets the line-ending sequence used in the output.
    ///     Defaults to <see cref="Environment.NewLine" />.
    /// </summary>
    public string NewLine { get; init; } = Environment.NewLine;

    /// <summary>
    ///     Gets a value indicating whether a blank line is emitted before each block
    ///     in clean-format mode (<see cref="RoundTrip" /> = <see langword="false" />).
    ///     Has no effect in round-trip mode.
    ///     Defaults to <see langword="true" />.
    /// </summary>
    public bool BlankLineBetweenBlocks { get; init; } = true;

    /// <summary>Gets the default options: clean format, four-space indent, space separator.</summary>
    public static SshSerializerOptions Default { get; } = new();

    /// <summary>Gets a round-trip options instance that preserves original formatting verbatim.</summary>
    public static SshSerializerOptions RoundTripMode { get; } = new() { RoundTrip = true };
}