using Material.Icons;
using OpenSSH_GUI.Dialogs.Enums;

namespace OpenSSH_GUI.Dialogs.Models;

/// <summary>
///     Parameters for displaying a standard message box.
/// </summary>
public class MessageBoxParams
{
    /// <summary>
    ///     Gets or sets the dialog title bar text.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets the message body.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    ///     Gets or sets which button set to display.
    /// </summary>
    public MessageBoxButtons Buttons { get; set; } = MessageBoxButtons.Ok;

    /// <summary>
    ///     Gets or sets the icon shown beside the message.
    /// </summary>
    public MaterialIconKind? Icon { get; set; }
}