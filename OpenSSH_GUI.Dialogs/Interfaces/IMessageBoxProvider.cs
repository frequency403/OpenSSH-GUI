using Material.Icons;
using OpenSSH_GUI.Dialogs.Enums;
using OpenSSH_GUI.Dialogs.Models;

namespace OpenSSH_GUI.Dialogs.Interfaces;

public interface IMessageBoxProvider
{
    /// <summary>
    ///     Shows a modal message box and returns the button the user clicked.
    /// </summary>
    /// <param name="title">The dialog title bar text.</param>
    /// <param name="message">The message body.</param>
    /// <param name="buttons">Which button set to display.</param>
    /// <param name="icon">Optional icon shown beside the message.</param>
    /// <returns>
    ///     A <see cref="MessageBoxResult" /> indicating which button was clicked,
    ///     or <see cref="MessageBoxResult.None" /> if the window was closed via the
    ///     title-bar chrome.
    /// </returns>
    Task<MessageBoxResult> ShowMessageBoxAsync(
        string title,
        string message,
        MessageBoxButtons buttons = MessageBoxButtons.Ok,
        MaterialIconKind icon = MaterialIconKind.ErrorOutline);

    /// <summary>
    ///     Shows a modal message box and returns the button the user clicked.
    /// </summary>
    /// <param name="params">The parameters for the message box.</param>
    /// <returns>
    ///     A <see cref="MessageBoxResult" /> indicating which button was clicked,
    ///     or <see cref="MessageBoxResult.None" /> if the window was closed via the
    ///     title-bar chrome.
    /// </returns>
    Task<MessageBoxResult> ShowMessageBoxAsync(MessageBoxParams @params);

    public Task<MessageBoxResult> ShowErrorMessageBoxAsync(Exception? e = null, string? customMessage = null);

    public Task<bool> ShowRetryMessageBoxAsync(Func<Task<bool?>> tryActionAsync, string title, string message,
        MaterialIconKind icon = MaterialIconKind.ErrorOutline, int retries = 3, bool showTryCountInTitle = true);
    
    /// <summary>
    ///     Shows a modal secure-input (password) prompt and returns the result.
    /// </summary>
    /// <param name="title">The dialog title bar text.</param>
    /// <param name="prompt">Descriptive label shown above the input field.</param>
    /// <param name="minLength">
    ///     Minimum character count; defaults to <c>1</c>.
    ///     Pass <c>0</c> to allow an empty input.
    /// </param>
    /// <param name="maxLength">
    ///     Maximum character count; defaults to <c>0</c> (unlimited).
    /// </param>
    /// <returns>
    ///     A <see cref="SecureInputResult" /> whose <see cref="SecureInputResult.Value" />
    ///     contains the UTF-8 encoded password, or <c>null</c> when the user cancels.
    ///     The caller is responsible for disposing the result to zero the buffer.
    /// </returns>
    Task<SecureInputResult?> ShowSecureInputAsync(
        string title,
        string prompt,
        int minLength = 1,
        int maxLength = 0);

    /// <summary>
    ///     Shows a modal secure-input (password) prompt and returns the result.
    /// </summary>
    /// <param name="params">The parameters for the secure-input prompt.</param>
    /// <returns>
    ///     A <see cref="SecureInputResult" /> whose <see cref="SecureInputResult.Value" />
    ///     contains the by the <see cref="SecureInputParams.Encoding"/> encoded password, or <c>null</c> when the user cancels.
    ///     The caller is responsible for disposing the result to zero the buffer.
    /// </returns>
    Task<SecureInputResult?> ShowSecureInputAsync(SecureInputParams @params);

    /// <summary>
    ///     Shows a modal text-input dialog with live validation.
    /// </summary>
    /// <param name="title">The dialog title bar text.</param>
    /// <param name="prompt">Descriptive label shown above the input field.</param>
    /// <param name="validator">
    ///     A function receiving the current input text. Return <c>null</c> when valid,
    ///     or an error message string to display when invalid.
    /// </param>
    /// <param name="initialValue">Optional pre-filled value.</param>
    /// <param name="watermark">Optional placeholder text.</param>
    /// <returns>
    ///     A <see cref="ValidatedInputResult" /> whose <see cref="ValidatedInputResult.Value" />
    ///     contains the confirmed text, or <c>null</c> when the user cancels.
    /// </returns>
    Task<ValidatedInputResult?> ShowValidatedInputAsync(
        string title,
        string prompt,
        Func<string, string?> validator,
        string initialValue = "",
        string watermark = "Enter value…");

    /// <summary>
    ///     Shows a modal text-input dialog with live validation.
    /// </summary>
    /// <param name="params">The parameters for the validated text-input prompt.</param>
    /// <returns>
    ///     A <see cref="ValidatedInputResult" /> whose <see cref="ValidatedInputResult.Value" />
    ///     contains the confirmed text, or <c>null</c> when the user cancels.
    /// </returns>
    Task<ValidatedInputResult?> ShowValidatedInputAsync(ValidatedInputParams @params);
}