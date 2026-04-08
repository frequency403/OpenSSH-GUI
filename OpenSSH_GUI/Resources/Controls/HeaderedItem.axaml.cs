using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Metadata;

namespace OpenSSH_GUI.Resources.Controls;

public partial class HeaderedItem : ContentControl
{
    public static readonly StyledProperty<object?> HeaderProperty =
        AvaloniaProperty.Register<HeaderedItem, object?>(nameof(Header));
    
    public static readonly StyledProperty<object?> SideHeaderProperty =
        AvaloniaProperty.Register<HeaderedItem, object?>(nameof(SideHeader));
    
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

    public HeaderedItem()
    {
        InitializeComponent();
    }
}