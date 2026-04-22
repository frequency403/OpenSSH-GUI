using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace OpenSSH_GUI.Resources.Controls;

public partial class HeaderedItem : ContentControl
{
    public static readonly StyledProperty<object?> HeaderProperty =
        AvaloniaProperty.Register<HeaderedItem, object?>(nameof(Header));

    public static readonly StyledProperty<object?> SideHeaderProperty =
        AvaloniaProperty.Register<HeaderedItem, object?>(nameof(SideHeader));

    // Header alignment
    public static readonly StyledProperty<HorizontalAlignment> HeaderHorizontalAlignmentProperty =
        AvaloniaProperty.Register<HeaderedItem, HorizontalAlignment>(
            nameof(HeaderHorizontalAlignment), HorizontalAlignment.Left);

    public static readonly StyledProperty<VerticalAlignment> HeaderVerticalAlignmentProperty =
        AvaloniaProperty.Register<HeaderedItem, VerticalAlignment>(
            nameof(HeaderVerticalAlignment), VerticalAlignment.Center);

    // SideHeader alignment
    public static readonly StyledProperty<HorizontalAlignment> SideHeaderHorizontalAlignmentProperty =
        AvaloniaProperty.Register<HeaderedItem, HorizontalAlignment>(
            nameof(SideHeaderHorizontalAlignment), HorizontalAlignment.Right);

    public static readonly StyledProperty<VerticalAlignment> SideHeaderVerticalAlignmentProperty =
        AvaloniaProperty.Register<HeaderedItem, VerticalAlignment>(
            nameof(SideHeaderVerticalAlignment), VerticalAlignment.Center);
    
    public HeaderedItem()
    {
        InitializeComponent();
    }

    public object? Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    public object? SideHeader
    {
        get => GetValue(SideHeaderProperty);
        set => SetValue(SideHeaderProperty, value);
    }

    public HorizontalAlignment HeaderHorizontalAlignment
    {
        get => GetValue(HeaderHorizontalAlignmentProperty);
        set => SetValue(HeaderHorizontalAlignmentProperty, value);
    }

    public VerticalAlignment HeaderVerticalAlignment
    {
        get => GetValue(HeaderVerticalAlignmentProperty);
        set => SetValue(HeaderVerticalAlignmentProperty, value);
    }

    public HorizontalAlignment SideHeaderHorizontalAlignment
    {
        get => GetValue(SideHeaderHorizontalAlignmentProperty);
        set => SetValue(SideHeaderHorizontalAlignmentProperty, value);
    }

    public VerticalAlignment SideHeaderVerticalAlignment
    {
        get => GetValue(SideHeaderVerticalAlignmentProperty);
        set => SetValue(SideHeaderVerticalAlignmentProperty, value);
    }
}