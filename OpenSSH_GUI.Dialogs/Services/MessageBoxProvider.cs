using Avalonia.Controls;
using Material.Icons;
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
    public Task<MessageBoxResult> ShowMessageBoxAsync(
        string title,
        string message,
        MessageBoxButtons buttons = MessageBoxButtons.Ok,
        MaterialIconKind icon = MaterialIconKind.ErrorOutline)
    {
        return ShowMessageBoxAsync(new MessageBoxParams
        {
            Title = title,
            Message = message,
            Buttons = buttons,
            Icon =  icon
        });
    }

    /// <inheritdoc />
    public Task<MessageBoxResult> ShowMessageBoxAsync(MessageBoxParams @params)
    {
        var dialog = new MessageBoxDialog(@params);
        return dialog.ShowDialog<MessageBoxResult>(owner);
    }

    public Task<MessageBoxResult> ShowErrorMessageBoxAsync(Exception? e = null, string? customMessage = null) =>
        ShowMessageBoxAsync(new MessageBoxParams()
        {
            Title = e?.GetType().Name ?? "Error",
            Message = e switch
            {
                not null when !string.IsNullOrWhiteSpace(customMessage) => string.Join(" ", customMessage, e.Message),
                null when !string.IsNullOrWhiteSpace(customMessage) => customMessage,
                not null => e.ToString(),
                _ => string.Empty
            },
            Buttons = MessageBoxButtons.Ok,
            Icon = MaterialIconKind.ErrorOutline,
        });

    public async Task<bool> ShowRetryMessageBoxAsync(Func<Task<bool?>> tryActionAsync, string title, string message, MaterialIconKind icon = MaterialIconKind.ErrorOutline, int retries = 3, bool showTryCountInTitle = true)
    {
        var tryCount = 1;
        
        while (tryCount <= retries)
        {
            if (showTryCountInTitle)
                title = string.Join(" ", title, string.Join("/", tryCount, retries));
            if (await tryActionAsync() is null or true)
                return true;
            if (await ShowMessageBoxAsync(title, message, MessageBoxButtons.OkCancel, icon) is MessageBoxResult.Cancel)
                return true;
            tryCount++;
        }
        return tryCount <= retries;
    }

    /// <inheritdoc />
    public Task<SecureInputResult?> ShowSecureInputAsync(
        string title,
        string prompt,
        int minLength = 1,
        int maxLength = 0) =>
        ShowSecureInputAsync(new SecureInputParams
        {
            Title = title,
            Prompt = prompt,
            MinLength = minLength,
            MaxLength = maxLength
        });

    /// <inheritdoc />
    public Task<SecureInputResult?> ShowSecureInputAsync(SecureInputParams @params)
    {
        var dialog = new SecureInputDialog(@params);
        return dialog.ShowDialog<SecureInputResult?>(owner);
    }

    /// <inheritdoc />
    public Task<ValidatedInputResult?> ShowValidatedInputAsync(
        string title,
        string prompt,
        Func<string, string?> validator,
        string initialValue = "",
        string watermark = "Enter value…")
    {
        return ShowValidatedInputAsync(new ValidatedInputParams
        {
            Title = title,
            Prompt = prompt,
            Validator = validator,
            InitialValue = initialValue,
            Watermark = watermark
        });
    }

    /// <inheritdoc />
    public Task<ValidatedInputResult?> ShowValidatedInputAsync(ValidatedInputParams @params)
    {
        var dialog = new ValidatedInputDialog(@params);
        return dialog.ShowDialog<ValidatedInputResult?>(owner);
    }
}