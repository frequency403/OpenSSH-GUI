using System.Diagnostics.CodeAnalysis;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Interfaces.Hosts;
using OpenSSH_GUI.Core.Interfaces.Services;

namespace OpenSSH_GUI.Core.Services;

public class ClipboardService(ILogger<ClipboardService> logger, IClipboardHost clipboardHost)
    : IClipboardService
{
    [MemberNotNullWhen(true, nameof(Clipboard))]
    private bool CanUseClipboard => clipboardHost is { Clipboard: not null };

    /// <inheritdoc />
    public IClipboard? Clipboard => CanUseClipboard ? clipboardHost.Clipboard : null;

    // --- Write ---
    /// <inheritdoc />
    public Task SetDataAsync(IAsyncDataTransfer dataTransfer)
    {
        return GetClipboard().SetDataAsync(dataTransfer);
    }

    /// <inheritdoc />
    public Task ClearAsync()
    {
        return GetClipboard().ClearAsync();
    }

    /// <inheritdoc />
    public Task FlushAsync()
    {
        return GetClipboard().FlushAsync();
    }

    /// <inheritdoc />
    public Task SetTextAsync(string text)
    {
        return GetClipboard().SetTextAsync(text);
    }

    /// <inheritdoc />
    public Task SetBitmapAsync(Bitmap bitmap)
    {
        return GetClipboard().SetBitmapAsync(bitmap);
    }

    /// <inheritdoc />
    public Task SetFileAsync(IStorageItem file)
    {
        return GetClipboard().SetFileAsync(file);
    }

    /// <inheritdoc />
    public Task SetFilesAsync(IEnumerable<IStorageItem> files)
    {
        return GetClipboard().SetFilesAsync(files);
    }

    /// <inheritdoc />
    public Task SetValueAsync<T>(DataFormat<T> format, T? value) where T : class
    {
        return GetClipboard().SetValueAsync(format, value);
    }

    /// <inheritdoc />
    public Task SetValuesAsync<T>(DataFormat<T> format, IEnumerable<T>? values) where T : class
    {
        return GetClipboard().SetValuesAsync(format, values);
    }

    // --- Read ---
    /// <inheritdoc />
    public Task<IAsyncDataTransfer?> TryGetDataAsync()
    {
        return GetClipboard().TryGetDataAsync();
    }

    /// <inheritdoc />
    public Task<IAsyncDataTransfer?> TryGetInProcessDataAsync()
    {
        return GetClipboard().TryGetInProcessDataAsync();
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<DataFormat>> GetDataFormatsAsync()
    {
        return GetClipboard().GetDataFormatsAsync();
    }

    /// <inheritdoc />
    public Task<Bitmap?> TryGetBitmapAsync()
    {
        return GetClipboard().TryGetBitmapAsync();
    }

    /// <inheritdoc />
    public Task<IStorageItem?> TryGetFileAsync()
    {
        return GetClipboard().TryGetFileAsync();
    }

    /// <inheritdoc />
    public Task<IStorageItem[]?> TryGetFilesAsync()
    {
        return GetClipboard().TryGetFilesAsync();
    }

    /// <inheritdoc />
    public Task<string?> TryGetTextAsync()
    {
        return GetClipboard().TryGetTextAsync();
        // ← war Bug
    }

    /// <inheritdoc />
    public Task<T?> TryGetValueAsync<T>(DataFormat<T> dataFormat) where T : class
    {
        return GetClipboard().TryGetValueAsync(dataFormat);
    }

    /// <inheritdoc />
    public Task<T[]?> TryGetValuesAsync<T>(DataFormat<T> dataFormat) where T : class
    {
        return GetClipboard().TryGetValuesAsync(dataFormat);
    }

    private IClipboard GetClipboard()
    {
        if (clipboardHost.Clipboard is { } clipboard)
            return clipboard;

        logger.LogError("Clipboard is unavailable on the main window.");
        throw new InvalidOperationException("Clipboard is not available on this window.");
    }
}