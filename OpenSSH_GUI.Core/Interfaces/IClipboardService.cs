using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;

namespace OpenSSH_GUI.Core.Interfaces;

public interface IClipboardService
{
    /// <inheritdoc cref="IClipboard"/>
    IClipboard? Clipboard { get; }

    /// <inheritdoc cref="IClipboard.SetDataAsync"/>
    Task SetDataAsync(IAsyncDataTransfer dataTransfer);

    /// <inheritdoc cref="IClipboard.ClearAsync"/>
    Task ClearAsync();

    /// <inheritdoc cref="IClipboard.FlushAsync"/>
    Task FlushAsync();

    /// <inheritdoc cref="IClipboard.SetTextAsync"/>
    Task SetTextAsync(string text);

    /// <inheritdoc cref="Avalonia.Input.Platform.ClipboardExtensions.SetBitmapAsync"/>
    Task SetBitmapAsync(Bitmap bitmap);

    /// <inheritdoc cref="Avalonia.Input.Platform.ClipboardExtensions.SetFileAsync"/>
    Task SetFileAsync(IStorageItem file);

    /// <inheritdoc cref="Avalonia.Input.Platform.ClipboardExtensions.SetFilesAsync"/>
    Task SetFilesAsync(IEnumerable<IStorageItem> files);

    /// <inheritdoc cref="Avalonia.Input.Platform.ClipboardExtensions.SetValueAsync"/>
    Task SetValueAsync<T>(DataFormat<T> format, T? value) where T : class;

    /// <inheritdoc cref="Avalonia.Input.Platform.ClipboardExtensions.SetValuesAsync"/>
    Task SetValuesAsync<T>(DataFormat<T> format, IEnumerable<T>? values) where T : class;

    /// <inheritdoc cref="IClipboard.TryGetDataAsync"/>
    Task<IAsyncDataTransfer?> TryGetDataAsync();

    /// <inheritdoc cref="IClipboard.TryGetInProcessDataAsync"/>
    Task<IAsyncDataTransfer?> TryGetInProcessDataAsync();

    /// <inheritdoc cref="Avalonia.Input.Platform.ClipboardExtensions.GetDataFormatsAsync"/>
    Task<IReadOnlyList<DataFormat>> GetDataFormatsAsync();

    /// <inheritdoc cref="Avalonia.Input.Platform.ClipboardExtensions.TryGetBitmapAsync"/>
    Task<Bitmap?> TryGetBitmapAsync();

    /// <inheritdoc cref="Avalonia.Input.Platform.ClipboardExtensions.TryGetFileAsync"/>
    Task<IStorageItem?> TryGetFileAsync();

    /// <inheritdoc cref="Avalonia.Input.Platform.ClipboardExtensions.TryGetFilesAsync"/>
    Task<IStorageItem[]?> TryGetFilesAsync();

    /// <inheritdoc cref="Avalonia.Input.Platform.ClipboardExtensions.TryGetTextAsync"/>
    Task<string?> TryGetTextAsync();

    /// <inheritdoc cref="Avalonia.Input.Platform.ClipboardExtensions.TryGetValueAsync"/>
    Task<T?> TryGetValueAsync<T>(DataFormat<T> dataFormat) where T : class;

    /// <inheritdoc cref="Avalonia.Input.Platform.ClipboardExtensions.TryGetValuesAsync"/>
    Task<T[]?> TryGetValuesAsync<T>(DataFormat<T> dataFormat) where T : class;
}