namespace OpenSSH_GUI.SshConfig;

/// <summary>
///     The exception that is thrown when an SSH configuration file contains invalid syntax.
/// </summary>
public sealed class SshConfigParseException : Exception
{
    /// <param name="message">Human-readable description of the syntax error.</param>
    /// <param name="line">1-based line number.</param>
    /// <param name="column">1-based column number.</param>
    /// <param name="filePath">Optional source-file path.</param>
    public SshConfigParseException(string message, int line, int column, string? filePath = null)
        : base(BuildMessage(message, line, column, filePath))
    {
        Line = line;
        Column = column;
        FilePath = filePath;
    }

    /// <summary>Gets the 1-based line number at which parsing failed.</summary>
    public int Line { get; }

    /// <summary>Gets the 1-based column number at which parsing failed.</summary>
    public int Column { get; }

    /// <summary>
    ///     Gets the path of the file in which the error occurred,
    ///     or <see langword="null" /> when parsing from a string.
    /// </summary>
    public string? FilePath { get; }

    private static string BuildMessage(string message, int line, int column, string? filePath)
    {
        var location = filePath is null
            ? $"({line},{column})"
            : $"{filePath}({line},{column})";

        return $"{location}: {message}";
    }
}