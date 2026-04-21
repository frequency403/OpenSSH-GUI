using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace OpenSSH_GUI.Resources.Controls;

public class FlyoutButton : ContentControl
{
    public static readonly StyledProperty<FlyoutBase?> FlyoutProperty =
        AvaloniaProperty.Register<FlyoutButton, FlyoutBase?>(nameof(Flyout));

    public FlyoutBase? Flyout
    {
        get => GetValue(FlyoutProperty);
        set => SetValue(FlyoutProperty, value);
    }

    public FlyoutButton()
    {
        AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
        AddHandler(PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Tunnel);
        AddHandler(PointerEnteredEvent, (_, _) => PseudoClasses.Set(":pointerover", true));
        AddHandler(PointerExitedEvent, (_, _) =>
        {
            PseudoClasses.Set(":pointerover", false);
            PseudoClasses.Set(":pressed", false);
        });
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        PseudoClasses.Set(":pressed", true);

        if (Flyout is { } flyout)
        {
            flyout.ShowAt(this);
            e.Handled = true;
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        PseudoClasses.Set(":pressed", false);
    }
}