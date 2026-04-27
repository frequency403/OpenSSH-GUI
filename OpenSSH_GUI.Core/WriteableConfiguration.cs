using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using OpenSSH_GUI.Core.Configuration;
using OpenSSH_GUI.Core.Interfaces;

namespace OpenSSH_GUI.Core;

public sealed class WritableConfiguration<T>(
    JsonFileConfigurationWriter writer,
    IOptionsMonitor<T> options) : IWritableConfiguration<T>
    where T : class, new()
{
    public T Current => options.CurrentValue;

    public async Task UpdateAsync(Action<T> update, CancellationToken ct = default)
    {
        await writer.UpdateAsync(async root =>
        {
            var current = root.Deserialize<T>() ?? new T();

            update(current);

            var newNode = JsonSerializer.SerializeToNode(current);
            root.Clear();

            if (newNode is JsonObject obj)
            {
                foreach (var kv in obj)
                    root[kv.Key] = kv.Value;
            }

            await Task.CompletedTask;
        }, ct);
    }

    public async Task SetAsync<TValue>(string key, TValue value, CancellationToken ct = default)
    {
        await writer.UpdateAsync(root =>
        {
            var node = JsonSerializer.SerializeToNode(value);
            JsonFileConfigurationWriter.Set(root, key, node);
            return Task.CompletedTask;
        }, ct);
    }
}