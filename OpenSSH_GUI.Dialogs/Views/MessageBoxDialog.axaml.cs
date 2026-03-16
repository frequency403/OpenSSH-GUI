using Avalonia.Controls;
using Avalonia.Interactivity;
using Material.Icons;
using OpenSSH_GUI.Dialogs.Enums;
using OpenSSH_GUI.Dialogs.Models;

namespace OpenSSH_GUI.Dialogs.Views;

/// <summary>
/// A general-purpose modal message box dialog for Avalonia.
/// Supports configurable button sets and optional icons.
/// Use <see cref="MessageBoxProvider"/> for convenient async access.
/// </summary>
public partial class MessageBoxDialog : Window
{
    /// <summary>
    /// Initialises a new <see cref="MessageBoxDialog"/> with the provided content and configuration.
    /// </summary>
    /// <param name="title">The window title bar text.</param>
    /// <param name="message">The message body shown to the user.</param>
    /// <param name="buttons">Which button set to display. Defaults to <see cref="MessageBoxButtons.Ok"/>.</param>
    /// <param name="icon">Optional icon shown to the left of the message. Defaults to <see cref="MessageBoxIcon.None"/>.</param>
    public MessageBoxDialog(
        string title,
        string message,
        MessageBoxButtons buttons = MessageBoxButtons.Ok,
        MessageBoxIcon icon = MessageBoxIcon.None)
    {
        InitializeComponent();

        Title = title;
        PART_Message.Text = message;

        ApplyButtons(buttons);
        ApplyIcon(icon);
    }

    /// <summary>
    /// Initialises a new <see cref="MessageBoxDialog"/> with the provided <see cref="MessageBoxParams"/>.
    /// </summary>
    /// <param name="params">The parameters for the message box.</param>
    public MessageBoxDialog(MessageBoxParams @params)
    {
        InitializeComponent();

        Title = @params.Title;
        PART_Message.Text = @params.Message;

        ApplyButtons(@params.Buttons);
        
        if (@params.Icon.HasValue)
            ApplyIcon(@params.Icon);
        else
            ApplyIcon(@params.LegacyIcon);
    }

    // -------------------------------------------------------------------------
    //  Private helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Sets the visibility of named button controls according to the requested button set.
    /// </summary>
    private void ApplyButtons(MessageBoxButtons buttons)
    {
        switch (buttons)
        {
            case MessageBoxButtons.Ok:
                PART_OkButton.IsVisible = true;
                break;

            case MessageBoxButtons.OkCancel:
                PART_OkButton.IsVisible = true;
                PART_CancelButton.IsVisible = true;
                break;

            case MessageBoxButtons.YesNo:
                PART_YesButton.IsVisible = true;
                PART_NoButton.IsVisible = true;
                break;

            case MessageBoxButtons.YesNoCancel:
                PART_YesButton.IsVisible = true;
                PART_NoButton.IsVisible = true;
                PART_CancelButton.IsVisible = true;
                break;
        }
    }

    /// <summary>
    /// Applies the icon glyph and colour that correspond to the requested <paramref name="icon"/> type.
    /// Uses Unicode symbols so no external icon library is required.
    /// </summary>
    private void ApplyIcon(MessageBoxIcon icon)
    {
        if (icon == MessageBoxIcon.None) return;

        PART_Icon.IsVisible = true;

        (PART_Icon.Text, PART_Icon.Foreground) = icon switch
        {
            MessageBoxIcon.Information => ("ℹ", Avalonia.Media.Brushes.DodgerBlue),
            MessageBoxIcon.Warning     => ("⚠", Avalonia.Media.Brushes.Orange),
            MessageBoxIcon.Error       => ("✖", Avalonia.Media.Brushes.Crimson),
            MessageBoxIcon.Question    => ("?", Avalonia.Media.Brushes.MediumSlateBlue),
            _                          => (string.Empty, Avalonia.Media.Brushes.Transparent)
        };
    }

    /// <summary>
    /// Applies the <see cref="MaterialIconKind"/> to the dialog.
    /// </summary>
    /// <param name="icon">The icon kind to display.</param>
    private void ApplyIcon(MaterialIconKind? icon)
    {
        if (icon == null) return;

        PART_MaterialIcon.IsVisible = true;
        PART_MaterialIcon.Kind = icon.Value;
    }

    // -------------------------------------------------------------------------
    //  Button click handlers
    // -------------------------------------------------------------------------

    private void OnYesClick(object? sender, RoutedEventArgs e)    => Close(MessageBoxResult.Yes);
    private void OnNoClick(object? sender, RoutedEventArgs e)     => Close(MessageBoxResult.No);
    private void OnOkClick(object? sender, RoutedEventArgs e)     => Close(MessageBoxResult.Ok);
    private void OnCancelClick(object? sender, RoutedEventArgs e) => Close(MessageBoxResult.Cancel);
}
