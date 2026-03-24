using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using Material.Icons.Avalonia;
using ReactiveUI.Avalonia;
using ReactiveUI.SourceGenerators;

namespace OpenSSH_GUI.Resources.Controls;

/// <summary>
/// A password input control with a toggleable visibility button (eye icon).
/// When hidden, input is masked with a bullet character; when revealed, plain text is shown.
/// </summary>
public partial class PasswordBox : UserControl
{
    // --- Avalonia Styled Properties ---

    /// <summary>Bindable property for the password value.</summary>
    public static readonly StyledProperty<string?> PasswordProperty =
        AvaloniaProperty.Register<PasswordBox, string?>(nameof(Password));

    /// <summary>Bindable property for the placeholder text.</summary>
    public static readonly StyledProperty<string?> WatermarkProperty =
        AvaloniaProperty.Register<PasswordBox, string?>(nameof(Watermark), "Password");

    public string? Password
    {
        get => GetValue(PasswordProperty);
        set => SetValue(PasswordProperty, value);
    }

    public string? Watermark
    {
        get => GetValue(WatermarkProperty);
        set => SetValue(WatermarkProperty, value);
    }
    
    // @TODO - Add a visibility changed event?
    

    // --- Parts ---
    private TextBox? _textBox;
    private ToggleButton? _toggle;
    private MaterialIcon? _eyeOpen;
    private MaterialIcon? _eyeClosed;

    public PasswordBox()
    {
        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        BindParts();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);

        // Resolve named parts after XAML is loaded
        _textBox   = this.FindControl<TextBox>("PART_TextBox")!;
        _toggle    = this.FindControl<ToggleButton>("PART_Toggle")!;
        _eyeOpen   = this.FindControl<MaterialIcon>("PART_EyeOpen")!;
        _eyeClosed = this.FindControl<MaterialIcon>("PART_EyeClosed")!;

        BindParts();
    }

    /// <summary>
    /// Wires up two-way binding between the internal TextBox and the Password property,
    /// and subscribes to the toggle button's checked state for visibility switching.
    /// </summary>
    private void BindParts()
    {
        if (_textBox is null || _toggle is null) return;

        // Two-way bind Password <-> TextBox.Text
        _textBox.Bind(TextBox.TextProperty,
            this.GetBindingObservable(PasswordProperty));

        _textBox.TextChanged += (_, _) =>
            Password = _textBox.Text;

        // Bind watermark
        _textBox.Bind(TextBox.WatermarkProperty,
            this.GetBindingObservable(WatermarkProperty));

        // Toggle eye icon and PasswordChar
        _toggle.IsCheckedChanged += OnToggleChanged;
    }

    /// <summary>
    /// Handles the eye-toggle state change.
    /// Clears <see cref="TextBox.PasswordChar"/> to reveal the text,
    /// or restores it to mask the input again.
    /// </summary>
    private void OnToggleChanged(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var isVisible = _toggle!.IsChecked == true;

        _textBox!.PasswordChar = isVisible ? '\0' : '●';

        if (_eyeOpen is not null)   _eyeOpen.IsVisible   = isVisible;
        if (_eyeClosed is not null) _eyeClosed.IsVisible = !isVisible;
    }
}