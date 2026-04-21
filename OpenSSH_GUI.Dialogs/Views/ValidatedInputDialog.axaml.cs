using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using OpenSSH_GUI.Dialogs.Models;

namespace OpenSSH_GUI.Dialogs.Views;

/// <summary>
///     A modal text-input dialog that validates the entered value in real-time via
///     an externally supplied <see cref="Func{T, TResult}" />.
/// </summary>
/// <remarks>
///     <para>
///         The caller provides a <c>Func&lt;string, string?&gt;</c> validator.  It receives the
///         current input text and should return:
///         <list type="bullet">
///             <item><c>null</c> – input is valid.</item>
///             <item>A non-empty <see cref="string" /> – the error message to display.</item>
///         </list>
///         The validator is evaluated on every keystroke so the user gets instant feedback.
///         The OK button is only enabled when the validator returns <c>null</c>.
///     </para>
/// </remarks>
public partial class ValidatedInputDialog : Window
{
    private readonly Func<string, string?> _validator;

    /// <summary>
    ///     Initialises a new <see cref="ValidatedInputDialog" />.
    /// </summary>
    /// <param name="title">The window title bar text.</param>
    /// <param name="prompt">Descriptive label shown above the input field.</param>
    /// <param name="validator">
    ///     A function that receives the current input text and returns <c>null</c> when
    ///     the value is valid, throws an <see cref="Exception" /> or returns an error message string otherwise.
    /// </param>
    /// <param name="initialValue">
    ///     Optional pre-filled value for the input field. Defaults to <see cref="string.Empty" />.
    /// </param>
    /// <param name="watermark">
    ///     Optional placeholder text shown when the input is empty.
    /// </param>
    public ValidatedInputDialog(
        string title,
        string prompt,
        Func<string, string?> validator,
        string initialValue = "",
        string watermark = "Enter value…")
    {
        InitializeComponent();

        _validator = validator ?? throw new ArgumentNullException(nameof(validator));

        Title = title;
        PART_Prompt.Text = prompt;
        PART_Prompt.IsVisible = !string.IsNullOrWhiteSpace(prompt);
        PART_Input.PlaceholderText = watermark;
        PART_Input.Text = initialValue;

        // Subscribe to live text changes for real-time validation.
        PART_Input.TextChanged += OnInputTextChanged;

        // Run initial validation so the OK button state is correct from the start.
        Validate();
    }

    /// <summary>
    ///     Initialises a new <see cref="ValidatedInputDialog" /> with the provided <see cref="ValidatedInputParams" />.
    /// </summary>
    /// <param name="params">The parameters for the validated text-input prompt.</param>
    public ValidatedInputDialog(ValidatedInputParams @params)
    {
        InitializeComponent();

        _validator = @params.Validator ?? throw new ArgumentNullException(nameof(@params.Validator));

        Title = @params.Title;
        PART_Prompt.Text = @params.Prompt;
        PART_Prompt.IsVisible = !string.IsNullOrWhiteSpace(@params.Prompt);

        if (@params.Icon.HasValue)
        {
            PART_MaterialIcon.IsVisible = true;
            PART_MaterialIcon.Kind = @params.Icon.Value;
        }

        PART_Input.PlaceholderText = @params.Watermark;
        PART_Input.Text = @params.InitialValue;

        // Subscribe to live text changes for real-time validation.
        PART_Input.TextChanged += OnInputTextChanged;

        // Run initial validation so the OK button state is correct from the start.
        Validate();
    }

    // -------------------------------------------------------------------------
    //  Window events
    // -------------------------------------------------------------------------

    private void OnOpened(object? sender, EventArgs e)
    {
        PART_Input.Focus();
        PART_Input.SelectAll();
    }

    // -------------------------------------------------------------------------
    //  Validation
    // -------------------------------------------------------------------------

    private void OnInputTextChanged(object? sender, TextChangedEventArgs e)
    {
        Validate();
    }

    /// <summary>
    ///     Runs the external validator against the current input and updates the
    ///     error label and OK button state accordingly.
    /// </summary>
    private void Validate()
    {
        var text = PART_Input.Text ?? string.Empty;
        try
        {
            var error = _validator(text);

            if (error is null)
            {
                HideError();
                PART_OkButton.IsEnabled = true;
            }
            else
            {
                ShowError(error);
                PART_OkButton.IsEnabled = false;
            }
        }
        catch (Exception e)
        {
            ShowError(e.Message);
            PART_OkButton.IsEnabled = false;
        }
    }

    // -------------------------------------------------------------------------
    //  Key / Button handlers
    // -------------------------------------------------------------------------

    private void OnInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && PART_OkButton.IsEnabled)
        {
            e.Handled = true;
            TryConfirm();
        }
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        TryConfirm();
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(new ValidatedInputResult(null));
    }

    // -------------------------------------------------------------------------
    //  Private helpers
    // -------------------------------------------------------------------------

    private void TryConfirm()
    {
        var text = PART_Input.Text ?? string.Empty;
        var error = _validator(text);

        if (error is not null)
        {
            ShowError(error);
            return;
        }

        Close(new ValidatedInputResult(text));
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
}