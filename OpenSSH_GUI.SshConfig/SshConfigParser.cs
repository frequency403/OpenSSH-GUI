using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;

namespace OpenSSH_GUI.SshConfig;

/// <summary>
///     Parses SSH client configuration files (<c>~/.ssh/config</c>) into a structured
///     <see cref="SshConfigDocument" />, fully resolving all <c>Include</c> directives.
/// </summary>
/// <remarks>
///     <para>
///         The parser accepts the full <c>ssh_config(5)</c> syntax including:
///         <list type="bullet">
///             <item>
///                 <description><c>Host</c> blocks with wildcard and negation patterns</description>
///             </item>
///             <item>
///                 <description><c>Match</c> blocks with all standard criteria</description>
///             </item>
///             <item>
///                 <description><c>Include</c> directives with glob expansion and tilde resolution, at any nesting level</description>
///             </item>
///             <item>
///                 <description>Both <c>Key Value</c> and <c>Key=Value</c> separator styles</description>
///             </item>
///             <item>
///                 <description>Double-quoted values containing spaces</description>
///             </item>
///             <item>
///                 <description>Inline comments (<c># …</c>) on directive lines</description>
///             </item>
///             <item>
///                 <description>Standalone comment lines and blank lines (preserved for round-trip)</description>
///             </item>
///         </list>
///     </para>
///     <para>
///         <c>Include</c> directives are resolved during parsing; the resulting
///         <see cref="SshConfigDocument" /> is flat and contains no <c>Include</c> entries.
///     </para>
/// </remarks>
public static class SshConfigParser
{
    // ─────────────────────────────────────────────────────────────────────────
    // Match criteria parser
    // ─────────────────────────────────────────────────────────────────────────

    private static readonly FrozenSet<string> MatchKeywords =
        FrozenSet.Create(StringComparer.OrdinalIgnoreCase,
            "all", "canonical", "final",
            "exec", "host", "originalhost",
            "user", "localuser", "tagged", "localnetwork",
            "address", "group", "localaddress", "localport", "port", "rdomain");

    private static readonly FrozenSet<string> NoArgMatchKeywords =
        FrozenSet.Create(StringComparer.OrdinalIgnoreCase,
            "all", "canonical", "final");
    // ─────────────────────────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    ///     Parses the SSH configuration file at <paramref name="path" />.
    /// </summary>
    /// <param name="path">Absolute or relative path to the configuration file.</param>
    /// <param name="options">Parser options, or <see langword="null" /> to use <see cref="SshConfigParserOptions.Default" />.</param>
    /// <returns>The fully parsed and include-resolved <see cref="SshConfigDocument" />.</returns>
    /// <exception cref="SshConfigParseException">The file contains a syntax error.</exception>
    /// <exception cref="IOException">The file could not be read.</exception>
    public static SshConfigDocument Load(string path, SshConfigParserOptions? options = null)
    {
        var fullPath = Path.GetFullPath(path);
        var content = File.ReadAllText(fullPath);
        return ParseDocument(content, fullPath, options ?? SshConfigParserOptions.Default, 0);
    }

    /// <summary>
    ///     Asynchronously parses the SSH configuration file at <paramref name="path" />.
    /// </summary>
    /// <param name="path">Absolute or relative path to the configuration file.</param>
    /// <param name="options">Parser options, or <see langword="null" /> to use <see cref="SshConfigParserOptions.Default" />.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    public static async Task<SshConfigDocument> LoadAsync(
        string path,
        SshConfigParserOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var fullPath = Path.GetFullPath(path);
        var content = await File.ReadAllTextAsync(fullPath, cancellationToken).ConfigureAwait(false);
        return ParseDocument(content, fullPath, options ?? SshConfigParserOptions.Default, 0);
    }

    /// <summary>
    ///     Parses SSH configuration content from a string.
    ///     <c>Include</c> directives are resolved relative to
    ///     <see cref="SshConfigParserOptions.IncludeBasePath" /> when set, or the current directory.
    /// </summary>
    /// <param name="content">Raw configuration text.</param>
    /// <param name="options">Parser options, or <see langword="null" /> to use <see cref="SshConfigParserOptions.Default" />.</param>
    public static SshConfigDocument Parse(string content, SshConfigParserOptions? options = null)
    {
        return ParseDocument(content, null, options ?? SshConfigParserOptions.Default, 0);
    }

    /// <summary>
    ///     Parses SSH configuration content from a <see cref="ReadOnlySpan{T}" /> of characters.
    /// </summary>
    /// <param name="content">Raw configuration characters.</param>
    /// <param name="options">Parser options, or <see langword="null" /> to use <see cref="SshConfigParserOptions.Default" />.</param>
    public static SshConfigDocument Parse(ReadOnlySpan<char> content, SshConfigParserOptions? options = null)
    {
        return ParseDocument(content.ToString(), null, options ?? SshConfigParserOptions.Default, 0);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Core parse loop
    // ─────────────────────────────────────────────────────────────────────────

    private static SshConfigDocument ParseDocument(
        string content,
        string? filePath,
        SshConfigParserOptions options,
        int depth)
    {
        var globalItems = ImmutableArray.CreateBuilder<SshLineItem>();
        var blocks = ImmutableArray.CreateBuilder<SshBlock>();
        BlockBuilder? currentBlock = null;
        Queue<SshBlock> pendingBlocks = new(); // blocks deferred from Include-inside-block
        var lineNumber = 0;

        foreach (var rawLine in content.AsSpan().EnumerateLines())
        {
            lineNumber++;
            var rawLineStr = rawLine.ToString();
            var trimmed = rawLine.TrimStart();

            if (trimmed.IsEmpty)
            {
                AddToContext(new SshBlankLine(lineNumber), globalItems, currentBlock);
                continue;
            }

            if (trimmed[0] == '#')
            {
                AddToContext(
                    new SshCommentLine(trimmed.ToString(), lineNumber, rawLineStr),
                    globalItems, currentBlock);
                continue;
            }

            var (key, values, inlineComment) = TokenizeLine(trimmed, lineNumber, filePath);
            var normalizedKey = SshKnownKeys.Normalize(key);

            switch (normalizedKey)
            {
                case "Host":
                    FinalizeBlock(ref currentBlock, blocks, pendingBlocks);
                    currentBlock = new BlockBuilder
                    {
                        IsHost = true,
                        HostPatterns = values,
                        LineNumber = lineNumber,
                        RawHeaderText = rawLineStr,
                        HeaderComment = inlineComment
                    };
                    break;

                case "Match":
                    FinalizeBlock(ref currentBlock, blocks, pendingBlocks);
                    currentBlock = new BlockBuilder
                    {
                        IsHost = false,
                        MatchCriteria = ParseMatchCriteria(values, lineNumber, filePath),
                        LineNumber = lineNumber,
                        RawHeaderText = rawLineStr,
                        HeaderComment = inlineComment
                    };
                    break;

                case "Include":
                    var included = ResolveInclude(values, filePath, options, depth, lineNumber);
                    if (currentBlock is null)
                    {
                        globalItems.AddRange(included.GlobalItems);
                        blocks.AddRange(included.Blocks);
                    }
                    else
                    {
                        // Include inside a block: its global-scope items join the current block;
                        // its own blocks are deferred until after the current block is finalised.
                        currentBlock.Items.AddRange(included.GlobalItems);
                        foreach (var deferred in included.Blocks)
                            pendingBlocks.Enqueue(deferred);
                    }

                    break;

                default:
                    if (options.ThrowOnUnknownKey && !SshKnownKeys.IsKnownKey(normalizedKey))
                        throw new SshConfigParseException(
                            $"Unknown keyword '{key}'.", lineNumber, 1, filePath);

                    AddToContext(
                        new SshConfigEntry(normalizedKey, values, inlineComment, lineNumber, rawLineStr),
                        globalItems, currentBlock);
                    break;
            }
        }

        FinalizeBlock(ref currentBlock, blocks, pendingBlocks);
        return new SshConfigDocument(globalItems.ToImmutable(), blocks.ToImmutable());
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Line tokenizer
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    ///     Tokenizes a single trimmed, non-empty, non-comment configuration line into
    ///     its key, value tokens, and optional inline comment.
    /// </summary>
    private static (string Key, ImmutableArray<string> Values, string? InlineComment) TokenizeLine(
        ReadOnlySpan<char> trimmedLine,
        int lineNumber,
        string? filePath)
    {
        var keyEnd = 0;
        while (keyEnd < trimmedLine.Length
               && trimmedLine[keyEnd] != ' '
               && trimmedLine[keyEnd] != '\t'
               && trimmedLine[keyEnd] != '=')
            keyEnd++;

        if (keyEnd == 0)
            throw new SshConfigParseException(
                "Expected a configuration keyword.", lineNumber, 1, filePath);

        var key = trimmedLine[..keyEnd].ToString();
        var rest = trimmedLine[keyEnd..].TrimStart();

        if (!rest.IsEmpty && rest[0] == '=')
            rest = rest[1..].TrimStart();

        var values = ImmutableArray.CreateBuilder<string>();
        string? inlineComment = null;

        while (!rest.IsEmpty)
        {
            if (rest[0] == '#')
            {
                inlineComment = rest.ToString();
                break;
            }

            string token;
            int advance;

            if (rest[0] == '"')
            {
                var closeQuote = rest[1..].IndexOf('"');
                if (closeQuote < 0)
                    throw new SshConfigParseException(
                        "Unterminated quoted string.",
                        lineNumber,
                        trimmedLine.Length - rest.Length + 1,
                        filePath);

                token = rest[1..(closeQuote + 1)].ToString();
                advance = closeQuote + 2;
            }
            else
            {
                var end = FindTokenEnd(rest);
                token = rest[..end].ToString();
                advance = end;
            }

            values.Add(token);
            rest = rest[advance..].TrimStart();
        }

        return (key, values.ToImmutable(), inlineComment);
    }

    /// <summary>
    ///     Returns the length of the next unquoted token in <paramref name="span" />,
    ///     stopping at the first whitespace character.
    /// </summary>
    private static int FindTokenEnd(ReadOnlySpan<char> span)
    {
        for (var i = 0; i < span.Length; i++)
            if (span[i] == ' ' || span[i] == '\t')
                return i;
        return span.Length;
    }

    /// <summary>
    ///     Parses the token list from a <c>Match</c> header line into an ordered array of
    ///     <see cref="SshMatchCriterion" /> objects.
    /// </summary>
    private static ImmutableArray<SshMatchCriterion> ParseMatchCriteria(
        ImmutableArray<string> tokens,
        int lineNumber,
        string? filePath)
    {
        if (tokens.IsEmpty)
            throw new SshConfigParseException(
                "A 'Match' block requires at least one criterion.", lineNumber, 7, filePath);

        var criteria = ImmutableArray.CreateBuilder<SshMatchCriterion>(tokens.Length);
        var i = 0;

        while (i < tokens.Length)
        {
            var keyword = tokens[i];

            if (!MatchKeywords.Contains(keyword))
                throw new SshConfigParseException(
                    $"Unknown Match criterion '{keyword}'.", lineNumber, 1, filePath);

            if (NoArgMatchKeywords.Contains(keyword))
            {
                criteria.Add(keyword.ToLowerInvariant() switch
                {
                    "all" => SshMatchCriterion.All,
                    "canonical" => SshMatchCriterion.Canonical,
                    "final" => SshMatchCriterion.Final,
                    _ => throw new UnreachableException()
                });
                i++;
            }
            else
            {
                if (i + 1 >= tokens.Length)
                    throw new SshConfigParseException(
                        $"Match criterion '{keyword}' requires a pattern argument.",
                        lineNumber, 1, filePath);

                var pattern = tokens[i + 1];
                var kind = keyword.ToLowerInvariant() switch
                {
                    "exec" => SshMatchCriterionKind.Exec,
                    "host" => SshMatchCriterionKind.Host,
                    "originalhost" => SshMatchCriterionKind.OriginalHost,
                    "user" => SshMatchCriterionKind.User,
                    "localuser" => SshMatchCriterionKind.LocalUser,
                    "tagged" => SshMatchCriterionKind.Tagged,
                    "localnetwork" => SshMatchCriterionKind.LocalNetwork,
                    "address" => SshMatchCriterionKind.Address,
                    "group" => SshMatchCriterionKind.Group,
                    "localaddress" => SshMatchCriterionKind.LocalAddress,
                    "localport" => SshMatchCriterionKind.LocalPort,
                    "port" => SshMatchCriterionKind.Port,
                    "rdomain" => SshMatchCriterionKind.RDomain,
                    _ => throw new UnreachableException()
                };

                criteria.Add(new SshMatchCriterion(kind, pattern));
                i += 2;
            }
        }

        return criteria.ToImmutable();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Include resolution
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    ///     Resolves and recursively parses all files referenced by the value tokens of an
    ///     <c>Include</c> directive.  Patterns are glob-expanded in alphabetical order,
    ///     consistent with OpenSSH behaviour.
    /// </summary>
    private static SshConfigDocument ResolveInclude(
        ImmutableArray<string> patterns,
        string? currentFilePath,
        SshConfigParserOptions options,
        int depth,
        int lineNumber)
    {
        if (depth >= options.MaxIncludeDepth)
            throw new SshConfigParseException(
                $"Maximum Include depth ({options.MaxIncludeDepth}) exceeded.",
                lineNumber, 1, currentFilePath);

        var basePath = options.IncludeBasePath
                       ?? (currentFilePath is not null ? Path.GetDirectoryName(currentFilePath) : null)
                       ?? Directory.GetCurrentDirectory();

        var globalItems = ImmutableArray.CreateBuilder<SshLineItem>();
        var blocks = ImmutableArray.CreateBuilder<SshBlock>();

        foreach (var rawPattern in patterns)
        {
            var expanded = options.ExpandTilde ? ExpandTilde(rawPattern) : rawPattern;

            var fullPattern = Path.IsPathRooted(expanded)
                ? expanded
                : Path.Combine(basePath, expanded);

            var dir = Path.GetDirectoryName(fullPattern) ?? basePath;
            var filePattern = Path.GetFileName(fullPattern);

            if (!Directory.Exists(dir))
                continue;

            foreach (var file in Directory.GetFiles(dir, filePattern).Order(StringComparer.Ordinal))
            {
                var fileContent = File.ReadAllText(file);
                var included = ParseDocument(fileContent, file, options, depth + 1);
                globalItems.AddRange(included.GlobalItems);
                blocks.AddRange(included.Blocks);
            }
        }

        return new SshConfigDocument(globalItems.ToImmutable(), blocks.ToImmutable());
    }

    /// <summary>
    ///     Replaces a leading <c>~</c> or <c>~/</c> with the current user's home directory.
    /// </summary>
    private static string ExpandTilde(string path)
    {
        if (path != "~" && !path.StartsWith("~/", StringComparison.Ordinal)
                        && !path.StartsWith("~\\", StringComparison.Ordinal)) return path;
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return path.Length == 1 ? home : Path.Combine(home, path[2..]);

    }

    // ─────────────────────────────────────────────────────────────────────────
    // Builder helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    ///     Builds and appends the current <see cref="BlockBuilder" /> to <paramref name="blocks" />,
    ///     then drains any blocks that were deferred from <c>Include</c> directives inside it.
    /// </summary>
    private static void FinalizeBlock(
        ref BlockBuilder? currentBlock,
        ImmutableArray<SshBlock>.Builder blocks,
        Queue<SshBlock> pendingBlocks)
    {
        if (currentBlock is null)
            return;

        blocks.Add(currentBlock.Build());
        currentBlock = null;

        while (pendingBlocks.TryDequeue(out var pending))
            blocks.Add(pending);
    }

    /// <summary>
    ///     Appends <paramref name="item" /> to the current block's items when inside a block,
    ///     or to the global items list when at the top level.
    /// </summary>
    private static void AddToContext(
        SshLineItem item,
        ImmutableArray<SshLineItem>.Builder globalItems,
        BlockBuilder? currentBlock)
    {
        if (currentBlock is not null)
            currentBlock.Items.Add(item);
        else
            globalItems.Add(item);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // BlockBuilder — mutable accumulator for a single Host / Match block
    // ─────────────────────────────────────────────────────────────────────────

    private sealed class BlockBuilder
    {
        public required bool IsHost { get; init; }
        public ImmutableArray<string> HostPatterns { get; init; } = [];
        public ImmutableArray<SshMatchCriterion> MatchCriteria { get; init; } = [];
        public required int LineNumber { get; init; }
        public required string RawHeaderText { get; init; }
        public string? HeaderComment { get; init; }
        public ImmutableArray<SshLineItem>.Builder Items { get; } = ImmutableArray.CreateBuilder<SshLineItem>();

        public SshBlock Build()
        {
            return IsHost
                ? new SshHostBlock(HostPatterns, Items.ToImmutable(), LineNumber, RawHeaderText, HeaderComment)
                : new SshMatchBlock(MatchCriteria, Items.ToImmutable(), LineNumber, RawHeaderText, HeaderComment);
        }
    }
}