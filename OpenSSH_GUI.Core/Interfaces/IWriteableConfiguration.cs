namespace OpenSSH_GUI.Core.Interfaces;

public interface IWritableConfiguration<out T> where T : class, new()
{
    T Current { get; }

    Task UpdateAsync(Action<T> update, CancellationToken ct = default);

    Task SetAsync<TValue>(string key, TValue value, CancellationToken ct = default);
}