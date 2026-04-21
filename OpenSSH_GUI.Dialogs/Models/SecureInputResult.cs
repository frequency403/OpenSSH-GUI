using System.Security.Cryptography;
using OpenSSH_GUI.Dialogs.Views;

namespace OpenSSH_GUI.Dialogs.Models;

/// <summary>
///     Holds the result of a <see cref="SecureInputDialog" /> as a UTF-8 encoded byte buffer.
///     The buffer is cryptographically zeroed when this instance is disposed.
/// </summary>
/// <remarks>
///     Always wrap usage in a <c>using</c> block or call <see cref="Dispose" /> explicitly
///     as soon as the credential is no longer needed to minimise its lifetime in memory.
/// </remarks>
public sealed class SecureInputResult : IDisposable
{
    private readonly byte[] _buffer;
    private bool _disposed;

    /// <summary>
    ///     Initialises a new <see cref="SecureInputResult" /> from a raw byte buffer.
    ///     Ownership of <paramref name="buffer" /> is transferred to this instance.
    /// </summary>
    /// <param name="buffer">The UTF-8 encoded password bytes. Must not be <c>null</c>.</param>
    internal SecureInputResult(byte[] buffer)
    {
        _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
    }

    /// <summary>
    ///     Gets the UTF-8 encoded password bytes.
    ///     Returns <see cref="Memory{T}.Empty" /> after the instance has been disposed.
    /// </summary>
    public Memory<byte> Value => _disposed ? Memory<byte>.Empty : _buffer.AsMemory();

    /// <summary>
    ///     Zeros the underlying byte buffer using <see cref="CryptographicOperations.ZeroMemory" />
    ///     and marks the instance as disposed.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        CryptographicOperations.ZeroMemory(_buffer);
        _disposed = true;
    }
}