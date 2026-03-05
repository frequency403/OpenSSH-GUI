namespace OpenSSH_GUI.SshConfig;

/// <summary>
/// Controls how <see cref="SshConfigParser"/> locates, reads, and validates
/// SSH configuration files.
/// </summary>
public sealed record SshConfigParserOptions
{
    /// <summary>
    /// Gets the base directory used to resolve relative paths in <c>Include</c> directives.
    /// <para>
    /// When <see langword="null"/>, the parser uses the directory of the file being parsed.
    /// This property is only relevant when parsing from a string via
    /// <see cref="SshConfigParser.Parse(string, SshConfigParserOptions?)"/>.
    /// </para>
    /// </summary>
    public string? IncludeBasePath { get; init; }

    /// <summary>
    /// Gets the maximum number of nested <c>Include</c> levels that will be followed
    /// before a <see cref="SshConfigParseException"/> is thrown to prevent infinite recursion.
    /// Defaults to <c>16</c>.
    /// </summary>
    public int MaxIncludeDepth { get; init; } = 16;

    /// <summary>
    /// Gets a value indicating whether a leading <c>~</c> or <c>~/</c> in <c>Include</c>
    /// paths is expanded to the current user's home directory.
    /// Defaults to <see langword="true"/>.
    /// </summary>
    public bool ExpandTilde { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether encountering an unrecognised keyword causes a
    /// <see cref="SshConfigParseException"/> to be thrown.
    /// When <see langword="false"/>, unknown keywords are silently accepted and stored verbatim.
    /// Defaults to <see langword="false"/>.
    /// </summary>
    public bool ThrowOnUnknownKey { get; init; }

    /// <summary>Gets the default options instance.</summary>
    public static SshConfigParserOptions Default { get; } = new();

    /// <summary>
    /// Gets a strict options instance that throws on any unrecognised keyword.
    /// </summary>
    public static SshConfigParserOptions Strict { get; } = new() { ThrowOnUnknownKey = true };
}
