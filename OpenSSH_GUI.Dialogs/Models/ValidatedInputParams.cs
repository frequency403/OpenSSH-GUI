namespace OpenSSH_GUI.Dialogs.Models;

/// <summary>
/// Parameters for displaying a text-input dialog with live validation.
/// </summary>
public class ValidatedInputParams : MessageBoxParams
{
    /// <summary>
    /// Gets or sets the descriptive label shown above the input field.
    /// </summary>
    public string Prompt { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the validation function.
    /// Returns <c>null</c> when valid, or an error message string when invalid.
    /// </summary>
    public Func<string, string?> Validator { get; set; } = _ => null;

    /// <summary>
    /// Gets or sets the pre-filled value for the input field.
    /// </summary>
    public string InitialValue { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the placeholder text shown when the input is empty.
    /// </summary>
    public string Watermark { get; set; } = "Enter value…";
}
