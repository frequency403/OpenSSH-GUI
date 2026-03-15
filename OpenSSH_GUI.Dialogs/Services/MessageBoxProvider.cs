using Avalonia.Controls;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Dialogs.Enums;
using OpenSSH_GUI.Dialogs.Interfaces;
using OpenSSH_GUI.Dialogs.Models;
using OpenSSH_GUI.Dialogs.Views;

namespace OpenSSH_GUI.Dialogs.Services;

/// <summary>
/// Provides static factory methods for showing application dialogs.
/// All methods must be called from the UI thread.
/// </summary>
public class MessageBoxProvider(Window owner) : IMessageBoxProvider
{
    /// <summary>
    /// Shows a modal message box and returns the button the user clicked.
    /// </summary>
    /// <param name="title">The dialog title bar text.</param>
    /// <param name="message">The message body.</param>
    /// <param name="buttons">Which button set to display.</param>
    /// <param name="icon">Optional icon shown beside the message.</param>
    /// <returns>
    /// A <see cref="MessageBoxResult"/> indicating which button was clicked,
    /// or <see cref="MessageBoxResult.None"/> if the window was closed via the
    /// title-bar chrome.
    /// </returns>
    public async Task<MessageBoxResult> ShowMessageBoxAsync(
        string title,
        string message,
        MessageBoxButtons buttons = MessageBoxButtons.Ok,
        MessageBoxIcon icon = MessageBoxIcon.None)
    {
        var dialog = new MessageBoxDialog(title, message, buttons, icon);
        return await dialog.ShowDialog<MessageBoxResult>(owner);
    }

    /// <summary>
    /// Shows a modal secure-input (password) prompt and returns the result.
    /// </summary>
    /// <param name="title">The dialog title bar text.</param>
    /// <param name="prompt">Descriptive label shown above the input field.</param>
    /// <param name="minLength">
    /// Minimum character count; defaults to <c>1</c>.
    /// Pass <c>0</c> to allow an empty input.
    /// </param>
    /// <param name="maxLength">
    /// Maximum character count; defaults to <c>0</c> (unlimited).
    /// </param>
    /// <returns>
    /// A <see cref="SecureInputResult"/> whose <see cref="SecureInputResult.Value"/>
    /// contains the UTF-8 encoded password, or <c>null</c> when the user cancels.
    /// The caller is responsible for disposing the result to zero the buffer.
    /// </returns>
    public async Task<SecureInputResult?> ShowSecureInputAsync(
        string title,
        string prompt,
        int minLength = 1,
        int maxLength = 0)
    {
        var dialog = new SecureInputDialog(title, prompt, minLength, maxLength);
        return await dialog.ShowDialog<SecureInputResult?>(owner);
    }
}
