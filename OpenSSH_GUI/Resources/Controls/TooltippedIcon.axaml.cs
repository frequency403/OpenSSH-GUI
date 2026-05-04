using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Material.Icons;

namespace OpenSSH_GUI.Resources.Controls;

public partial class TooltippedIcon : UserControl
{
    public static readonly StyledProperty<MaterialIconKind> IconProperty =
        AvaloniaProperty.Register<TooltippedIcon, MaterialIconKind>(nameof(Icon), MaterialIconKind.Info);
    
    public static readonly StyledProperty<object?> ToolTipContentProperty =
        AvaloniaProperty.Register<TooltippedIcon, object?>(nameof(ToolTipContent), null);
    
    public static readonly StyledProperty<PlacementMode> ToolTipPlacementProperty =
        AvaloniaProperty.Register<TooltippedIcon, PlacementMode>(nameof(ToolTipPlacement), PlacementMode.Bottom);

    public object? ToolTipContent
    {
        get => GetValue(ToolTipContentProperty);
        set => SetValue(ToolTipContentProperty, value);
    }

    public MaterialIconKind Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }
    
    public PlacementMode ToolTipPlacement
    {
        get => GetValue(ToolTipPlacementProperty);
        set => SetValue(ToolTipPlacementProperty, value);
    }
    
    
    public TooltippedIcon() { InitializeComponent(); }
}