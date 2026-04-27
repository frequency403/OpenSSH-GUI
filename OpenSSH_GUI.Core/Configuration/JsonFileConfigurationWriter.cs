using System.Text.Json;
using System.Text.Json.Nodes;

namespace OpenSSH_GUI.Core.Configuration;

public sealed class JsonFileConfigurationWriter(string filePath)
{
    private readonly SemaphoreSlim _lock = new(1, 1);

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true
    };

    public async Task<JsonObject> ReadAsync(CancellationToken ct)
    {
        if (!File.Exists(filePath))
            return new JsonObject();

        await using var stream = File.OpenRead(filePath);
        var node = await JsonNode.ParseAsync(stream, cancellationToken: ct);

        return node as JsonObject ?? new JsonObject();
    }

    public async Task WriteAsync(JsonObject root, CancellationToken ct)
    {
        var tempFile = filePath + ".tmp";

        await using (var stream = File.Create(tempFile))
        {
            await JsonSerializer.SerializeAsync(stream, root, Options, ct);
        }

        File.Move(tempFile, filePath, overwrite: true);
    }

    public async Task UpdateAsync(Func<JsonObject, Task> update, CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var root = await ReadAsync(ct);

            await update(root);

            await WriteAsync(root, ct);
        }
        finally
        {
            _lock.Release();
        }
    }


    public static void Set(JsonObject root, string key, JsonNode? value)
    {
        var parts = key.Split(':');
        JsonObject current = root;

        for (int i = 0; i < parts.Length - 1; i++)
        {
            if (current[parts[i]] is not JsonObject next)
            {
                next = new JsonObject();
                current[parts[i]] = next;
            }

            current = next;
        }

        current[parts[^1]] = value;
    }
}