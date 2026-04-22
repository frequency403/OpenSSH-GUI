using Avalonia.Controls;
using Avalonia.Interactivity;
using Material.Icons;
using OpenSSH_GUI.Dialogs.Enums;
using OpenSSH_GUI.Dialogs.Models;
using OpenSSH_GUI.Dialogs.Services;

namespace OpenSSH_GUI.Dialogs.Views;

/// <summary>
///     A general-purpose modal message box dialog for Avalonia.
///     Supports configurable button sets and optional icons.
///     Use <see cref="MessageBoxProvider" /> for convenient async access.
/// </summary>
public partial class MessageBoxDialog : Window
{
    /// <summary>
    ///     Initialises a new <see cref="MessageBoxDialog" /> with the provided <see cref="MessageBoxParams" />.
    /// </summary>
    /// <param name="params">The parameters for the message box.</param>
    public MessageBoxDialog(MessageBoxParams @params)
    {
        InitializeComponent();

        Title = @params.Title;
        PART_Message.Text = @params.Message;

        ApplyButtons(@params.Buttons);

        ApplyIcon(@params.Icon);
    }

    // -------------------------------------------------------------------------
    //  Private helpers
    // -------------------------------------------------------------------------

    /// <summary>
    ///     Sets the visibility of named button controls according to the requested button set.
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
    ///     Applies the <see cref="MaterialIconKind" /> to the dialog.
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

    private void OnYesClick(object? sender, RoutedEventArgs e)
    {
        Close(MessageBoxResult.Yes);
    }

    private void OnNoClick(object? sender, RoutedEventArgs e)
    {
        Close(MessageBoxResult.No);
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        Close(MessageBoxResult.Ok);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(MessageBoxResult.Cancel);
    }
    private bool _isInternalClose;
    private void Window_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_isInternalClose)
            return;

        e.Cancel = true;

        _isInternalClose = true;
        Close(MessageBoxResult.Cancel);
    }
}