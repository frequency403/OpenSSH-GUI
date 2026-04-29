using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace OpenSSH_GUI.Core.Configuration;

public sealed class JsonFileConfigurationWriter<T>(string filePath, JsonTypeInfo<T> typeInfo)
{
    private readonly SemaphoreSlim _lock = new(1, 1);

    /// <summary>
    /// Reads and deserializes the configuration file into <typeparamref name="T"/>.
    /// Returns a default instance if the file does not exist.
    /// </summary>
    public async Task<T?> ReadAsync(CancellationToken ct)
    {
        if (!File.Exists(filePath))
            return default;

        await using var stream = File.OpenRead(filePath);
        return await JsonSerializer.DeserializeAsync(stream, typeInfo, ct);
    }

    /// <summary>
    /// Atomically writes <paramref name="value"/> to the configuration file via a temp-file swap.
    /// </summary>
    public async Task WriteAsync(T value, CancellationToken ct)
    {
        var tempFile = Path.GetTempFileName();
        await using (var stream = File.Open(tempFile, FileMode.Truncate))
        {
            await JsonSerializer.SerializeAsync(stream, value, typeInfo, ct);
        }
        File.Move(tempFile, filePath, overwrite: true);
    }

    /// <summary>
    /// Reads the current configuration, applies <paramref name="update"/>, then writes the result back atomically.
    /// </summary>
    public async Task UpdateAsync(Func<T?, Task<T>> update, CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var current = await ReadAsync(ct);
            var updated = await update(current);
            await WriteAsync(updated, ct);
        }
        finally
        {
            _lock.Release();
        }
    }
}