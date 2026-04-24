using Avalonia.Controls;
using Avalonia.Media.Imaging;

namespace OpenSSH_GUI.Core.Resources;

/// <summary>
///     Holds pre-rendered app icons and window icons, keyed by a canonical string key.
///     Populated during Avalonia framework initialization before the main window is shown.
/// </summary>
public sealed class AppIconStore
{
    private readonly Dictionary<string, Bitmap> _bitmaps = new();
    private readonly Dictionary<string, WindowIcon> _windowIcons = new();

    /// <summary>Stores a rendered <see cref="Bitmap" /> under the given key.</summary>
    public void AddBitmap(string key, Bitmap bitmap) { _bitmaps[key] = bitmap; }

    /// <summary>Stores a <see cref="WindowIcon" /> under the given key.</summary>
    public void AddWindowIcon(string key, WindowIcon icon) { _windowIcons[key] = icon; }

    /// <summary>Retrieves a <see cref="Bitmap" /> by key, or <see langword="null" /> if not found.</summary>
    public Bitmap? GetBitmap(string key) => _bitmaps.GetValueOrDefault(key);

    /// <summary>Retrieves a <see cref="WindowIcon" /> by key, or <see langword="null" /> if not found.</summary>
    public WindowIcon? GetWindowIcon(string key) => _windowIcons.GetValueOrDefault(key);
}