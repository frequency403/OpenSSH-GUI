using System.Collections.Frozen;
using Avalonia.Controls;
using Avalonia.Media.Imaging;

namespace OpenSSH_GUI.Core.Resources;

/// <summary>
///     Holds pre-rendered app icons and window icons, keyed by a canonical string key.
///     Populated during Avalonia framework initialization before the main window is shown.
/// </summary>
public sealed class AppIconStore
{
    private FrozenDictionary<string, Bitmap> bitmaps = FrozenDictionary<string, Bitmap>.Empty;
    private FrozenDictionary<string, WindowIcon> windowIcons = FrozenDictionary<string, WindowIcon>.Empty;

    private Dictionary<string, Bitmap>? bitmapStage = new();
    private Dictionary<string, WindowIcon>? windowIconStage = new();

    /// <summary>Stores a rendered <see cref="Bitmap" /> under the given key.</summary>
    public void AddBitmap(string key, Bitmap bitmap)
    {
        if (bitmapStage is null) throw new InvalidOperationException("AppIconStore is already frozen.");
        bitmapStage[key] = bitmap;
    }
    
    /// <summary>Stores a <see cref="WindowIcon" /> under the given key.</summary>
    public void AddWindowIcon(string key, WindowIcon icon)
    {
        if (windowIconStage is null) throw new InvalidOperationException("AppIconStore is already frozen.");
        windowIconStage[key] = icon;
    }

    /// <summary>
    ///     Freezes both dictionaries
    /// </summary>
    public void Freeze()
    {
        bitmaps = bitmapStage?.ToFrozenDictionary() ?? FrozenDictionary<string, Bitmap>.Empty;
        windowIcons = windowIconStage?.ToFrozenDictionary() ?? FrozenDictionary<string, WindowIcon>.Empty;

        bitmapStage?.Clear();
        windowIconStage?.Clear();
        bitmapStage = null;
        windowIconStage = null;
    }
    
    /// <summary>Retrieves a <see cref="Bitmap" /> by key, or <see langword="null" /> if not found.</summary>
    public Bitmap? GetBitmap(string key) => bitmaps.GetValueOrDefault(key);

    /// <summary>Retrieves a <see cref="WindowIcon" /> by key, or <see langword="null" /> if not found.</summary>
    public WindowIcon? GetWindowIcon(string key) => windowIcons.GetValueOrDefault(key);
}