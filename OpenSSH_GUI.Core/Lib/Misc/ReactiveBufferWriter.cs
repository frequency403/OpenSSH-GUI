using System.Buffers;
using System.ComponentModel;
using ReactiveUI;

namespace OpenSSH_GUI.Core.Lib.Misc;

/// <summary>
/// A thread-safe, reactive wrapper around <see cref="ArrayBufferWriter{T}"/> 
/// implementing <see cref="ReactiveUI.IReactiveObject"/> for use with ReactiveUI bindings.
/// </summary>
/// <typeparam name="T">The element type of the buffer.</typeparam>
public sealed class ReactiveBufferWriter<T> : IReactiveObject, IBufferWriter<T>
{
    private readonly Lock _lockObject = new();
    private readonly ArrayBufferWriter<T> _inner;
    private static readonly string[] PropertyNames = [nameof(WrittenCount), nameof(WrittenMemory)];

    /// <summary>
    /// Initializes a new instance with an optional initial capacity.
    /// </summary>
    /// <param name="initialCapacity">Initial buffer capacity. Defaults to 256.</param>
    public ReactiveBufferWriter(int initialCapacity = 256)
        => _inner = new ArrayBufferWriter<T>(initialCapacity);

    /// <summary>Gets the portion of the buffer that has been written to.</summary>
    public ReadOnlyMemory<T> WrittenMemory { get { lock (_lockObject) return _inner.WrittenMemory; } }

    /// <summary>Gets the written data as a span.</summary>
    public ReadOnlySpan<T> WrittenSpan { get { lock (_lockObject) return _inner.WrittenSpan; } }

    /// <summary>Gets the number of committed elements.</summary>
    public int WrittenCount { get { lock (_lockObject) return _inner.WrittenCount; } }

    /// <inheritdoc/>
    public void Advance(int count)
    {
        RaiseAllChanging();
        lock (_lockObject)
        {
            _inner.Advance(count);
        }
        RaiseAllChanged();
    }

    /// <inheritdoc/>
    public Memory<T> GetMemory(int sizeHint = 0) { lock (_lockObject) return _inner.GetMemory(sizeHint); }

    /// <inheritdoc/>
    public Span<T> GetSpan(int sizeHint = 0) { lock (_lockObject) return _inner.GetSpan(sizeHint); }

    /// <summary>Resets the writer and notifies subscribers.</summary>
    public void Clear()
    {
        RaiseAllChanging();
        lock (_lockObject)
        {
            _inner.Clear();
        }
        RaiseAllChanged();
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event PropertyChangingEventHandler? PropertyChanging;

    private void RaiseAllChanging()
    {
        foreach (var publicPropertyName in PropertyNames)
        {
            this.RaisePropertyChanging(publicPropertyName);
        }
    }

    private void RaiseAllChanged()
    {
        foreach (var publicPropertyName in PropertyNames)
        {
            this.RaisePropertyChanged(publicPropertyName);
        }
    }
    
    void IReactiveObject.RaisePropertyChanging(PropertyChangingEventArgs args) 
        => PropertyChanging?.Invoke(this, args);
    void IReactiveObject.RaisePropertyChanged(PropertyChangedEventArgs args) 
        => PropertyChanged?.Invoke(this, args);
}