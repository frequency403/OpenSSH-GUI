namespace OpenSSH_GUI.Dialogs.Models;

/// <summary>
///     Holds the result of a <see cref="Views.ValidatedInputDialog" />.
///     <see cref="Value" /> contains the confirmed text, or is <c>null</c> when the user cancelled.
/// </summary>
/// <param name="Value">The validated user input, or <c>null</c> on cancel.</param>
public sealed record ValidatedInputResult(string? Value)
{
    /// <summary>
    ///     <c>true</c> when the user confirmed the dialog with a valid value.
    /// </summary>
    public bool IsConfirmed => Value is not null;
}