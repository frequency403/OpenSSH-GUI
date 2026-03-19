using Avalonia.Controls;
using OpenSSH_GUI.Dialogs.Enums;
using OpenSSH_GUI.Dialogs.Interfaces;
using OpenSSH_GUI.Dialogs.Models;
using OpenSSH_GUI.Dialogs.Views;

namespace OpenSSH_GUI.Dialogs.Services;

/// <summary>
///     Provides static factory methods for showing application dialogs.
///     All methods must be called from the UI thread.
/// </summary>
public class MessageBoxProvider(Window owner) : IMessageBoxProvider
{
    /// <inheritdoc />
    public async Task<MessageBoxResult> ShowMessageBoxAsync(
        string title,
        string message,
        MessageBoxButtons buttons = MessageBoxButtons.Ok,
        MessageBoxIcon icon = MessageBoxIcon.None)
    {
        return await ShowMessageBoxAsync(new MessageBoxParams
        {
            Title = title,
            Message = message,
            Buttons = buttons,
            LegacyIcon = icon
        });
    }

    /// <inheritdoc />
    public async Task<MessageBoxResult> ShowMessageBoxAsync(MessageBoxParams @params)
    {
        var dialog = new MessageBoxDialog(@params);
        return await dialog.ShowDialog<MessageBoxResult>(owner);
    }

    /// <inheritdoc />
    public async Task<SecureInputResult?> ShowSecureInputAsync(
        string title,
        string prompt,
        int minLength = 1,
        int maxLength = 0)
    {
        return await ShowSecureInputAsync(new SecureInputParams
        {
            Title = title,
            Prompt = prompt,
            MinLength = minLength,
            MaxLength = maxLength
        });
    }

    /// <inheritdoc />
    public async Task<SecureInputResult?> ShowSecureInputAsync(SecureInputParams @params)
    {
        var dialog = new SecureInputDialog(@params);
        return await dialog.ShowDialog<SecureInputResult?>(owner);
    }

    /// <inheritdoc />
    public async Task<ValidatedInputResult?> ShowValidatedInputAsync(
        string title,
        string prompt,
        Func<string, string?> validator,
        string initialValue = "",
        string watermark = "Enter value…")
    {
        return await ShowValidatedInputAsync(new ValidatedInputParams
        {
            Title = title,
            Prompt = prompt,
            Validator = validator,
            InitialValue = initialValue,
            Watermark = watermark
        });
    }

    /// <inheritdoc />
    public async Task<ValidatedInputResult?> ShowValidatedInputAsync(ValidatedInputParams @params)
    {
        var dialog = new ValidatedInputDialog(@params);
        return await dialog.ShowDialog<ValidatedInputResult?>(owner);
    }
}