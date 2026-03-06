using System.Diagnostics.CodeAnalysis;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Interfaces;

namespace OpenSSH_GUI.Core.Services;

public class ClipboardService(ILogger<ClipboardService> logger, IClipboardHost clipboardHost)
    : IClipboardService
{
    [MemberNotNullWhen(true, nameof(Clipboard))]
    private bool CanUseClipboard => clipboardHost is { Clipboard: not null };

    /// <inheritdoc/>
    public IClipboard? Clipboard => CanUseClipboard ? clipboardHost.Clipboard : null;

    private IClipboard GetClipboard()
    {
        if (clipboardHost.Clipboard is { } clipboard)
            return clipboard;

        logger.LogError("Clipboard is unavailable on the main window.");
        throw new InvalidOperationException("Clipboard is not available on this window.");
    }

    // --- Write ---
    /// <inheritdoc/>
    public Task SetDataAsync(IAsyncDataTransfer dataTransfer) => GetClipboard().SetDataAsync(dataTransfer);

    /// <inheritdoc/>
    public Task ClearAsync() => GetClipboard().ClearAsync();

    /// <inheritdoc/>
    public Task FlushAsync() => GetClipboard().FlushAsync();

    /// <inheritdoc/>
    public Task SetTextAsync(string text) => GetClipboard().SetTextAsync(text);

    /// <inheritdoc/>
    public Task SetBitmapAsync(Bitmap bitmap) => GetClipboard().SetBitmapAsync(bitmap);

    /// <inheritdoc/>
    public Task SetFileAsync(IStorageItem file) => GetClipboard().SetFileAsync(file);

    /// <inheritdoc/>
    public Task SetFilesAsync(IEnumerable<IStorageItem> files) => GetClipboard().SetFilesAsync(files);

    /// <inheritdoc/>
    public Task SetValueAsync<T>(DataFormat<T> format, T? value) where T : class
        => GetClipboard().SetValueAsync(format, value);

    /// <inheritdoc/>
    public Task SetValuesAsync<T>(DataFormat<T> format, IEnumerable<T>? values) where T : class
        => GetClipboard().SetValuesAsync(format, values);

    // --- Read ---
    /// <inheritdoc/>
    public Task<IAsyncDataTransfer?> TryGetDataAsync() => GetClipboard().TryGetDataAsync();

    /// <inheritdoc/>
    public Task<IAsyncDataTransfer?> TryGetInProcessDataAsync() => GetClipboard().TryGetInProcessDataAsync();

    /// <inheritdoc/>
    public Task<IReadOnlyList<DataFormat>> GetDataFormatsAsync() => GetClipboard().GetDataFormatsAsync();

    /// <inheritdoc/>
    public Task<Bitmap?> TryGetBitmapAsync() => GetClipboard().TryGetBitmapAsync();

    /// <inheritdoc/>
    public Task<IStorageItem?> TryGetFileAsync() => GetClipboard().TryGetFileAsync();

    /// <inheritdoc/>
    public Task<IStorageItem[]?> TryGetFilesAsync() => GetClipboard().TryGetFilesAsync();

    /// <inheritdoc/>
    public Task<string?> TryGetTextAsync() => GetClipboard().TryGetTextAsync(); // ← war Bug

    /// <inheritdoc/>
    public Task<T?> TryGetValueAsync<T>(DataFormat<T> dataFormat) where T : class
        => GetClipboard().TryGetValueAsync(dataFormat);

    /// <inheritdoc/>
    public Task<T[]?> TryGetValuesAsync<T>(DataFormat<T> dataFormat) where T : class
        => GetClipboard().TryGetValuesAsync(dataFormat);
}