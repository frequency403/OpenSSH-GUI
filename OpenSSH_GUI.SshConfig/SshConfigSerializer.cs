using System.Text;

namespace OpenSSH_GUI.SshConfig;

/// <summary>
/// Serializes a <see cref="SshConfigDocument"/> back to SSH configuration text,
/// supporting both lossless round-trip and clean-reformat modes.
/// </summary>
/// <remarks>
/// <para>
/// In <b>round-trip mode</b> (<see cref="SshSerializerOptions.RoundTrip"/> = <see langword="true"/>),
/// any item or block header with a non-empty <see cref="SshLineItem.RawText"/> /
/// <see cref="SshBlock.RawHeaderText"/> is written verbatim.  Items where <c>RawText</c>
/// is <see cref="string.Empty"/> are regenerated from structured data, enabling surgical
/// in-place edits without disturbing surrounding formatting.
/// </para>
/// <para>
/// In <b>clean mode</b> (the default), all output is regenerated using the formatting
/// parameters in <see cref="SshSerializerOptions"/>.
/// </para>
/// </remarks>
public static class SshConfigSerializer
{
    // ─────────────────────────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Writes <paramref name="document"/> to the file at <paramref name="path"/>,
    /// creating or overwriting it.
    /// </summary>
    /// <param name="document">The document to serialize.</param>
    /// <param name="path">Destination file path.</param>
    /// <param name="options">Serializer options, or <see langword="null"/> to use <see cref="SshSerializerOptions.Default"/>.</param>
    public static void Save(SshConfigDocument document, string path, SshSerializerOptions? options = null)
    {
        var content = Serialize(document, options);
        File.WriteAllText(path, content, Encoding.UTF8);
    }

    /// <summary>
    /// Asynchronously writes <paramref name="document"/> to the file at <paramref name="path"/>.
    /// </summary>
    /// <param name="document">The document to serialize.</param>
    /// <param name="path">Destination file path.</param>
    /// <param name="options">Serializer options, or <see langword="null"/> to use <see cref="SshSerializerOptions.Default"/>.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    public static async Task SaveAsync(
        SshConfigDocument       document,
        string                  path,
        SshSerializerOptions?   options           = null,
        CancellationToken       cancellationToken = default)
    {
        var content = Serialize(document, options);
        await File.WriteAllTextAsync(path, content, Encoding.UTF8, cancellationToken)
                  .ConfigureAwait(false);
    }

    /// <summary>
    /// Serializes <paramref name="document"/> to a string.
    /// </summary>
    /// <param name="document">The document to serialize.</param>
    /// <param name="options">Serializer options, or <see langword="null"/> to use <see cref="SshSerializerOptions.Default"/>.</param>
    public static string Serialize(SshConfigDocument document, SshSerializerOptions? options = null)
    {
        var opts = options ?? SshSerializerOptions.Default;
        var sb   = new StringBuilder();

        foreach (var item in document.GlobalItems)
            WriteItem(sb, item, indent: string.Empty, opts);

        var isFirstBlock = true;

        foreach (var block in document.Blocks)
        {
            if (!opts.RoundTrip && opts.BlankLineBetweenBlocks)
            {
                if (!isFirstBlock || document.GlobalItems.Length > 0)
                    sb.Append(opts.NewLine);
            }

            WriteBlock(sb, block, opts);
            isFirstBlock = false;
        }

        return sb.ToString();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Block serialization
    // ─────────────────────────────────────────────────────────────────────────

    private static void WriteBlock(StringBuilder sb, SshBlock block, SshSerializerOptions opts)
    {
        if (opts.RoundTrip && block.RawHeaderText.Length > 0)
            sb.Append(block.RawHeaderText);
        else
            sb.Append(BuildBlockHeader(block, opts));

        sb.Append(opts.NewLine);

        foreach (var item in block.Items)
            WriteItem(sb, item, opts.Indent, opts);
    }

    private static string BuildBlockHeader(SshBlock block, SshSerializerOptions opts)
    {
        var header = block switch
        {
            SshHostBlock  host  => $"Host {string.Join(' ', host.Patterns)}",
            SshMatchBlock match => $"Match {string.Join(' ', match.Criteria.Select(static c => c.ToString()))}",
            _                   => throw new ArgumentException($"Unsupported block type: {block.GetType().Name}"),
        };

        return block.HeaderComment is null ? header : $"{header} {block.HeaderComment}";
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Item serialization
    // ─────────────────────────────────────────────────────────────────────────

    private static void WriteItem(StringBuilder sb, SshLineItem item, string indent, SshSerializerOptions opts)
    {
        switch (item)
        {
            case SshBlankLine:
                sb.Append(opts.NewLine);
                break;

            case SshCommentLine comment:
                if (opts.RoundTrip && comment.RawText.Length > 0)
                    sb.Append(comment.RawText);
                else
                {
                    sb.Append(indent);
                    sb.Append(comment.Comment);
                }
                sb.Append(opts.NewLine);
                break;

            case SshConfigEntry entry:
                if (opts.RoundTrip && entry.RawText.Length > 0)
                    sb.Append(entry.RawText);
                else
                    sb.Append(BuildEntryLine(entry, indent, opts));
                sb.Append(opts.NewLine);
                break;
        }
    }

    private static string BuildEntryLine(SshConfigEntry entry, string indent, SshSerializerOptions opts)
    {
        var valueStr = string.Join(' ', entry.Values.Select(QuoteIfNeeded));
        var line     = $"{indent}{entry.Key}{opts.KeyValueSeparator}{valueStr}";

        return entry.InlineComment is null ? line : $"{line} {entry.InlineComment}";
    }

    /// <summary>
    /// Wraps <paramref name="value"/> in double quotes when it contains whitespace,
    /// preserving unquoted values that are already safe.
    /// </summary>
    private static string QuoteIfNeeded(string value) =>
        value.AsSpan().ContainsAny(' ', '\t') ? $"\"{value}\"" : value;
}
