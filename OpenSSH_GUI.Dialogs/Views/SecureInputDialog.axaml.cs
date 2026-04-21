using System.Text;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using OpenSSH_GUI.Dialogs.Models;

namespace OpenSSH_GUI.Dialogs.Views;

/// <summary>
///     A modal password-prompt dialog that stores the entered characters directly
///     into a UTF-8 byte buffer, bypassing .NET's managed string allocation as
///     much as Avalonia's text pipeline allows.
/// </summary>
/// <remarks>
///     <para>
///         Avalonia's <see cref="TextBox" /> internally represents its content as a
///         <see cref="string" />.  To minimise exposure, this dialog:
///         <list type="bullet">
///             <item>
///                 Intercepts <see cref="TextBox.TextInputEvent" /> at tunnel phase,
///                 marking the event handled so the default string accumulation is
///                 suppressed.
///             </item>
///             <item>
///                 Maintains its own <see cref="List{T}" /> of raw UTF-8 byte segments,
///                 assembled per typed character.
///             </item>
///             <item>
///                 Handles <see cref="Key.Back" /> and <see cref="Key.Delete" /> to
///                 remove the last encoded character from the buffer.
///             </item>
///         </list>
///         The resulting <see cref="SecureInputResult" /> owns the consolidated byte
///         array and zeros it via
///         <see cref="System.Security.Cryptography.CryptographicOperations.ZeroMemory" />
///         when disposed.
///     </para>
///     <para>
///         Note: The display <see cref="TextBox" /> still holds a string of bullet
///         characters (●), not the actual password, so no sensitive data appears there.
///     </para>
/// </remarks>
public partial class SecureInputDialog : Window
{
    private readonly Encoding _encoding = Encoding.UTF8;
    private readonly int _maxLength;
    private readonly int _minLength;

    // Each entry represents the UTF-8 encoding of one logical character typed
    // by the user.  Kept as separate segments so Backspace can remove the last
    // character correctly even for multi-byte code points.
    private readonly List<byte[]> _segments = new();

    /// <summary>
    ///     Initialises a new <see cref="SecureInputDialog" />.
    /// </summary>
    /// <param name="title">The window title bar text.</param>
    /// <param name="prompt">Descriptive text shown above the input field.</param>
    /// <param name="minLength">
    ///     Minimum number of characters required. <c>0</c> disables the lower-bound check.
    /// </param>
    /// <param name="maxLength">
    ///     Maximum number of characters allowed. <c>0</c> disables the upper-bound check.
    /// </param>
    public SecureInputDialog(
        string title,
        string prompt,
        int minLength = 1,
        int maxLength = 0)
    {
        InitializeComponent();

        Title = title;
        PART_Prompt.Text = prompt;
        PART_Prompt.IsVisible = !string.IsNullOrWhiteSpace(prompt);

        _minLength = minLength;
        _maxLength = maxLength;

        // Intercept at tunnel phase so the event is handled before the TextBox
        // appends the character to its own internal string buffer.
        PART_Input.AddHandler(
            TextInputEvent,
            OnInputTextInput,
            RoutingStrategies.Tunnel);
    }

    /// <summary>
    ///     Initialises a new <see cref="SecureInputDialog" /> with the provided <see cref="SecureInputParams" />.
    /// </summary>
    /// <param name="params">The parameters for the secure-input prompt.</param>
    public SecureInputDialog(SecureInputParams @params)
    {
        InitializeComponent();

        _encoding = @params.Encoding;
        Title = @params.Title;
        PART_Prompt.Text = @params.Prompt;
        PART_Prompt.IsVisible = !string.IsNullOrWhiteSpace(@params.Prompt);

        if (@params.Icon.HasValue)
        {
            PART_MaterialIcon.IsVisible = true;
            PART_MaterialIcon.Kind = @params.Icon.Value;
        }

        _minLength = @params.MinLength;
        _maxLength = @params.MaxLength;

        // Intercept at tunnel phase so the event is handled before the TextBox
        // appends the character to its own internal string buffer.
        PART_Input.AddHandler(
            TextInputEvent,
            OnInputTextInput,
            RoutingStrategies.Tunnel);
    }

    // -------------------------------------------------------------------------
    //  Window events
    // -------------------------------------------------------------------------

    /// <summary>
    ///     Moves keyboard focus to the password field once the window is shown.
    /// </summary>
    private void OnOpened(object? sender, EventArgs e)
    {
        PART_Input.Focus();
    }

    // -------------------------------------------------------------------------
    //  Secure input interception
    // -------------------------------------------------------------------------

    /// <summary>
    ///     Intercepts typed characters before the <see cref="TextBox" /> processes them,
    ///     encodes each character as UTF-8, appends it to <see cref="_segments" />,
    ///     and updates the bullet display.
    /// </summary>
    private void OnInputTextInput(object? sender, TextInputEventArgs e)
    {
        e.Handled = true; // suppress default TextBox string accumulation

        if (string.IsNullOrEmpty(e.Text)) return;
        if (_maxLength > 0 && _segments.Count >= _maxLength) return;

        // Encode each character individually so Backspace can remove exactly
        // one logical character at a time.
        foreach (var encoded in e.Text.Select(ch => _encoding.GetBytes([ch]))) _segments.Add(encoded);

        SyncDisplay();
        HideError();
    }

    /// <summary>
    ///     Handles <see cref="Key.Back" /> to remove the last character segment
    ///     and <see cref="Key.Enter" /> to confirm the dialog.
    /// </summary>
    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Back when _segments.Count > 0:
                e.Handled = true;
                var last = _segments[^1];
                Array.Clear(last, 0, last.Length); // zero the removed segment
                _segments.RemoveAt(_segments.Count - 1);
                SyncDisplay();
                break;

            case Key.Enter:
                e.Handled = true;
                TryConfirm();
                break;
        }
    }

    // -------------------------------------------------------------------------
    //  Button handlers
    // -------------------------------------------------------------------------

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        TryConfirm();
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        ZeroSegments();
        Close(null);
    }

    // -------------------------------------------------------------------------
    //  Private helpers
    // -------------------------------------------------------------------------

    /// <summary>
    ///     Validates the current input length, then consolidates all byte segments
    ///     into a single array, wraps it in a <see cref="SecureInputResult" />,
    ///     and closes the dialog with that result.
    /// </summary>
    private void TryConfirm()
    {
        if (_minLength > 0 && _segments.Count < _minLength)
        {
            ShowError($"Password must be at least {_minLength} character(s).");
            return;
        }

        var buffer = ConsolidateBuffer();
        ZeroSegments();

        Close(new SecureInputResult(buffer));
    }

    /// <summary>
    ///     Merges all <see cref="_segments" /> into one contiguous byte array.
    /// </summary>
    private byte[] ConsolidateBuffer()
    {
        var totalLength = 0;
        foreach (var seg in _segments) totalLength += seg.Length;

        var buffer = new byte[totalLength];
        var offset = 0;

        foreach (var seg in _segments)
        {
            seg.CopyTo(buffer, offset);
            offset += seg.Length;
        }

        return buffer;
    }

    /// <summary>
    ///     Zeros and clears all byte segments in <see cref="_segments" />.
    /// </summary>
    private void ZeroSegments()
    {
        foreach (var seg in _segments)
            Array.Clear(seg, 0, seg.Length);

        _segments.Clear();
    }

    /// <summary>
    ///     Keeps the display <see cref="TextBox" /> in sync with <see cref="_segments" />
    ///     by setting its text to a matching count of bullet characters.
    ///     The actual password is never written to the TextBox.
    /// </summary>
    private void SyncDisplay()
    {
        // Temporarily remove the TextInput handler while we set the display
        // text, so the bullet characters are not fed back into the interceptor.
        PART_Input.RemoveHandler(TextInputEvent, OnInputTextInput);
        PART_Input.Text = new string('●', _segments.Count);
        PART_Input.AddHandler(TextInputEvent, OnInputTextInput, RoutingStrategies.Tunnel);

        // Move caret to end
        PART_Input.CaretIndex = PART_Input.Text?.Length ?? 0;
    }

    private void ShowError(string message)
    {
        PART_Error.Text = message;
        PART_Error.IsVisible = true;
    }

    private void HideError()
    {
        PART_Error.IsVisible = false;
        PART_Error.Text = string.Empty;
    }

    private void Window_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        Close(null);
    }
}