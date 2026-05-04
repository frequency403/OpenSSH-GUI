using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Material.Icons;

namespace OpenSSH_GUI.Resources.Controls;

public partial class TooltippedIcon : UserControl
{
    public static readonly StyledProperty<MaterialIconKind> IconProperty =
        AvaloniaProperty.Register<TooltippedIcon, MaterialIconKind>(nameof(Icon), MaterialIconKind.Info);

    public static readonly StyledProperty<object?> ToolTipContentProperty =
        AvaloniaProperty.Register<TooltippedIcon, object?>(nameof(ToolTipContent));

    public static readonly StyledProperty<PlacementMode> ToolTipPlacementProperty =
        AvaloniaProperty.Register<TooltippedIcon, PlacementMode>(nameof(ToolTipPlacement), PlacementMode.Bottom);

    public MaterialIconKind Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public object? ToolTipContent
    {
        get => GetValue(ToolTipContentProperty);
        set => SetValue(ToolTipContentProperty, value);
    }

    public PlacementMode ToolTipPlacement
    {
        get => GetValue(ToolTipPlacementProperty);
        set => SetValue(ToolTipPlacementProperty, value);
    }

    private DispatcherTimer? _hoverTimer;
    private bool _isPinned;

    public TooltippedIcon()
    {
        InitializeComponent();
        InitHoverTimer();
    }

    /// <summary>
    /// Initializes the hover delay timer used to open the popup on prolonged pointer hover.
    /// </summary>
    private void InitHoverTimer()
    {
        _hoverTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
        _hoverTimer.Tick += OnHoverTimerTick;
    }

    /// <summary>
    /// Opens the popup transiently when the hover delay elapses, unless already pinned.
    /// </summary>
    private void OnHoverTimerTick(object? sender, EventArgs e)
    {
        _hoverTimer?.Stop();
        Popup.IsOpen = true;
    }

    /// <summary>
    /// Starts the hover timer when the pointer enters the hit area.
    /// </summary>
    private void OnPointerEntered(object? sender, PointerEventArgs e)
    {
        if (!_isPinned)
            _hoverTimer?.Start();
    }

    /// <summary>
    /// Closes the popup on pointer exit, unless it has been pinned via click.
    /// </summary>
    private void OnPointerExited(object? sender, PointerEventArgs e)
    {
        _hoverTimer?.Stop();
        if (_isPinned) return;
        
        var pos = e.GetPosition(Popup.Child);
        if (Popup.IsOpen && Popup.Child is not null)
        {
            var bounds = Popup.Child.Bounds;
            if (bounds.Contains(pos)) return;
        }

        Popup.IsOpen = false;
    }

    /// <summary>
    /// Pins the popup open on click, or unpins and closes it if already pinned.
    /// Light dismiss will also unpin via <see cref="OnPopupClosed"/>.
    /// </summary>
    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _hoverTimer?.Stop();
        _isPinned = !_isPinned;
        Popup.IsOpen = _isPinned;
    }

    /// <summary>
    /// Resets the pinned state when the popup is closed externally via light dismiss.
    /// </summary>
    private void OnPopupClosed(object? sender, EventArgs e)
    {
        _isPinned = false;
    }
    
    /// <summary>
    /// Closes the popup when the pointer leaves the popup content area, unless pinned.
    /// </summary>
    private void OnPopupContentExited(object? sender, PointerEventArgs e)
    {
        if (!_isPinned)
            Popup.IsOpen = false;
    }
}