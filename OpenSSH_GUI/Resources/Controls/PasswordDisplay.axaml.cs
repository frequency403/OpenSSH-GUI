using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using OpenSSH_GUI.Core.Lib.Keys;

namespace OpenSSH_GUI.Resources.Controls;

/// <summary>
///     A password input control with a toggleable visibility button (eye icon).
///     When hidden, input is masked with the configured <see cref="MaskCharacter" />;
///     when revealed, plain text is shown. Supports read-only mode and external
///     observation of the current visibility state via <see cref="PasswordVisible" />.
/// </summary>
public partial class PasswordDisplay : UserControl
{
    private const char DefaultMaskChar = '●';
    private const string DefaultWatermark = "Password";

    // --- Avalonia Styled Properties ---

    /// <summary>Bindable property for the placeholder text.</summary>
    public static readonly StyledProperty<string?> WatermarkProperty =
        AvaloniaProperty.Register<PasswordDisplay, string?>(nameof(Watermark), DefaultWatermark);

    /// <summary>Bindable property for the character used to mask the password input.</summary>
    public static readonly StyledProperty<char> MaskCharacterProperty =
        AvaloniaProperty.Register<PasswordDisplay, char>(nameof(MaskCharacter), DefaultMaskChar);

    /// <summary>
    ///     Bindable property indicating whether the password is currently visible.
    ///     Can be observed or driven externally (e.g., from a ViewModel) to react
    ///     to visibility changes — for instance, to show or enable a copy button.
    /// </summary>
    public static readonly StyledProperty<bool> PasswordVisibleProperty =
        AvaloniaProperty.Register<PasswordDisplay, bool>(nameof(PasswordVisible));


    public static readonly StyledProperty<SshKeyFilePassword?> SecurePasswordProperty =
        AvaloniaProperty.Register<PasswordDisplay, SshKeyFilePassword?>(nameof(SecurePassword));

    // --- Parts ---
    private TextBox _textBox = new();
    private ToggleButton _toggle = new();

    public PasswordDisplay()
    {
        InitializeComponent();
    }

    public SshKeyFilePassword? SecurePassword
    {
        get => GetValue(SecurePasswordProperty);
        set => SetValue(SecurePasswordProperty, value);
    }

    public string? Watermark
    {
        get => GetValue(WatermarkProperty);
        set => SetValue(WatermarkProperty, value);
    }

    public char MaskCharacter
    {
        get => GetValue(MaskCharacterProperty);
        set => SetValue(MaskCharacterProperty, value);
    }

    public bool PasswordVisible
    {
        get => GetValue(PasswordVisibleProperty);
        set => SetValue(PasswordVisibleProperty, value);
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SecurePasswordProperty && GetValue(SecurePasswordProperty) is { } property)
            _textBox.Text = property.GetPasswordString();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);

        _textBox = this.FindControl<TextBox>("PART_Password")!;
        _toggle = this.FindControl<ToggleButton>("PART_Toggle")!;
        _textBox.TextChanged += (_, _) => { _toggle.IsEnabled = _textBox.Text?.Length > 0; };
    }
}