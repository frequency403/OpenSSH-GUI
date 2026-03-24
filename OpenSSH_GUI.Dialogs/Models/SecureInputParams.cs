namespace OpenSSH_GUI.Dialogs.Models;

/// <summary>
///     Parameters for displaying a secure-input (password) prompt.
/// </summary>
public class SecureInputParams : MessageBoxParams
{
    /// <summary>
    ///     Gets or sets the descriptive label shown above the input field.
    /// </summary>
    public string Prompt { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the minimum character count.
    ///     Defaults to 1. Pass 0 to allow an empty input.
    /// </summary>
    public int MinLength { get; set; } = 1;

    /// <summary>
    ///     Gets or sets the maximum character count.
    ///     Defaults to 0 (unlimited).
    /// </summary>
    public int MaxLength { get; set; } = 0;
}