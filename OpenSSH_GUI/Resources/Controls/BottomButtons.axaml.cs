// File Created by: Oliver Schantz
// Created: 27.05.2024 - 14:05:56
// Last edit: 27.05.2024 - 14:05:56

using System.ComponentModel;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Material.Icons;

namespace OpenSSH_GUI.Resources.Controls;

/// <summary>
/// UserControl representing the bottom buttons in the OpenSSH GUI.
/// </summary>
public partial class BottomButtons : UserControl
{
    /// <summary>
    /// CommandProperty is a static styled property of type ICommand that represents the command to be executed when the buttons in the OpenSshGuiBottomButtons control are clicked.
    /// </summary>
    public static readonly StyledProperty<ICommand> CommandProperty =
        AvaloniaProperty.Register<BottomButtons, ICommand>(nameof(Command));

    /// <summary>
    /// Represents a styled property for enabling or disabling the submit functionality of the OpenSshGuiBottomButtons control.
    /// </summary>
    public static readonly StyledProperty<bool> SubmitEnabledProperty =
        AvaloniaProperty.Register<BottomButtons, bool>(nameof(SubmitEnabled), defaultValue: true);

    /// <summary>
    /// Gets or sets a value indicating whether the abort button is enabled.
    /// </summary>
    public static readonly StyledProperty<bool> AbortEnabledProperty =
        AvaloniaProperty.Register<BottomButtons, bool>(nameof(AbortEnabled), defaultValue: true);

    /// <summary>
    /// Represents a custom control for displaying bottom buttons in the OpenSSH GUI.
    /// </summary>
    public static readonly StyledProperty<MaterialIconKind> SubmitIconProperty =
        AvaloniaProperty.Register<BottomButtons, MaterialIconKind>(nameof(SubmitIcon), defaultValue: MaterialIconKind.FloppyDisc);

    /// <summary>
    /// The AbortIconProperty class represents the styled property for the abort icon in the OpenSshGuiBottomButtons control.
    /// </summary>
    public static readonly StyledProperty<MaterialIconKind> AbortIconProperty =
        AvaloniaProperty.Register<BottomButtons, MaterialIconKind>(nameof(AbortIcon), defaultValue: MaterialIconKind.Cancel);


    /// <summary>
    /// The tooltip property for the Submit button in the OpenSshGuiBottomButtons control.
    /// </summary>
    public static readonly StyledProperty<string> SubmitButtonToolTipProperty = AvaloniaProperty.Register<BottomButtons, string>(nameof(SubmitButtonToolTip), defaultValue: "Submit");

    /// <summary>
    /// The tooltip for the Abort button in the OpenSshGuiBottomButtons control.
    /// </summary>
    public static readonly StyledProperty<string> AbortButtonToolTipProperty = AvaloniaProperty.Register<BottomButtons, string>(nameof(AbortButtonToolTip), defaultValue: "Cancel");

    /// <summary>
    /// Gets or sets the tooltip text that appears when hovering over the submit button.
    /// </summary>
    /// <remarks>
    /// This property is used to set the tooltip for the submit button in the OpenSshGuiBottomButtons control.
    /// The tooltip provides additional information about the purpose or functionality of the submit button.
    /// </remarks>
    [Bindable(true)]
    public string SubmitButtonToolTip
    {
        get => GetValue(SubmitButtonToolTipProperty);
        set => SetValue(SubmitButtonToolTipProperty, value);
    }

    /// <summary>
    /// Gets or sets the tooltip text for the abort button.
    /// </summary>
    [Bindable(true)]
    public string AbortButtonToolTip
    {
        get => GetValue(AbortButtonToolTipProperty);
        set => SetValue(AbortButtonToolTipProperty, value);
    }


    /// <summary>
    /// Gets or sets the submit icon for the OpenSshGuiBottomButtons control.
    /// </summary>
    /// <remarks>
    /// This property represents the icon displayed on the submit button of the OpenSshGuiBottomButtons control.
    /// The icon is defined using the Material Icon font, specified by the MaterialIconKind enumeration.
    /// </remarks>
    [Bindable(true)]
    public MaterialIconKind SubmitIcon
    {
        get => GetValue(SubmitIconProperty);
        set => SetValue(SubmitIconProperty, value);
    }
    
    /// <summary>
    /// Gets or sets the icon for the Abort button.
    /// </summary>
    [Bindable(true)]
    public MaterialIconKind AbortIcon
    {
        get => GetValue(AbortIconProperty);
        set => SetValue(AbortIconProperty, value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether the submit button is enabled.
    /// </summary>
    [Bindable(true)]
    public bool SubmitEnabled
    {
        get => GetValue(SubmitEnabledProperty);
        set => SetValue(SubmitEnabledProperty, value);
    }

    [Bindable(true)]
    public bool AbortEnabled
    {
        get => GetValue(AbortEnabledProperty);
        set => SetValue(AbortEnabledProperty, value);
    }

    /// <summary>
    /// Represents a custom control that provides bottom buttons for a user interface.
    /// </summary>
    [Bindable(true)]
    public ICommand Command
    {
        get => GetValue(CommandProperty);
    }
    

    public BottomButtons()
    {
        InitializeComponent();
    }
}