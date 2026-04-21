using Avalonia;
using Avalonia.Controls;

namespace OpenSSH_GUI.Resources.Controls;

public partial class HeaderedItem : ContentControl
{
    public static readonly StyledProperty<object?> HeaderProperty =
        AvaloniaProperty.Register<HeaderedItem, object?>(nameof(Header));

    public static readonly StyledProperty<object?> SideHeaderProperty =
        AvaloniaProperty.Register<HeaderedItem, object?>(nameof(SideHeader));

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
}