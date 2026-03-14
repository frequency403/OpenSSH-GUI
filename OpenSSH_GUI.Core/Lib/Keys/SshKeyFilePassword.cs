using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace OpenSSH_GUI.Core.Lib.Keys;

/// <summary>
/// Provides secure, pinned-memory storage for an SSH key file passphrase.
/// All sensitive data is kept in a single pinned buffer and wiped on <see cref="Clear"/> or <see cref="Dispose"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Security contract:</b> This class never allocates the passphrase on the managed heap
/// (beyond what callers pass in as <see cref="string"/>). Callers who need the passphrase as
/// text should use <see cref="GetChars"/> with a stack-allocated <c>Span&lt;char&gt;</c> and
/// wipe it immediately after use.
/// </para>
/// <para>This class is <b>not</b> thread-safe.</para>
/// </remarks>
public sealed class SshKeyFilePassword : INotifyPropertyChanged, IDisposable
{
    /// <summary>
    /// Maximum passphrase size in bytes. Sufficient for any reasonable SSH passphrase.
    /// </summary>
    public const int MaxPasswordBytes = 1024;

    /// <summary>
    /// Pinned buffer that holds the passphrase bytes. Pinning prevents the GC from
    /// relocating the data, so <see cref="CryptographicOperations.ZeroMemory"/>
    /// can reliably wipe the only copy.
    /// </summary>
    private readonly byte[] _buffer = GC.AllocateArray<byte>(MaxPasswordBytes, pinned: true);

    private int _writtenCount;
    private Encoding _encoding = Encoding.UTF8;
    private bool _disposed;

    /// <summary>
    /// Creates an empty instance. Use <see cref="Set(ReadOnlySpan{byte},Encoding?)"/> to populate.
    /// </summary>
    internal SshKeyFilePassword() { }

    /// <summary>
    /// Initializes a new instance with an optional passphrase provided as raw bytes.
    /// </summary>
    /// <param name="password">The passphrase bytes, or <c>null</c> to create an empty instance.</param>
    /// <param name="encoding">Encoding used for byte↔char conversions. Defaults to UTF-8.</param>
    public SshKeyFilePassword(ReadOnlyMemory<byte>? password = null, Encoding? encoding = null)
    {
        if (encoding is not null)
            _encoding = encoding;
        if (password is { Length: > 0 } pass)
            WriteToBuffer(pass.Span);
    }

    /// <summary>
    /// Initializes a new instance from a managed string.
    /// </summary>
    /// <param name="password">
    /// The passphrase string. Note: the caller-provided <see cref="string"/> is inherently
    /// on the managed heap and cannot be wiped. Prefer the <c>ReadOnlyMemory&lt;byte&gt;</c>
    /// overload where possible.
    /// </param>
    /// <param name="encoding">Encoding used for byte↔char conversions. Defaults to UTF-8.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the encoded passphrase exceeds <see cref="MaxPasswordBytes"/>.
    /// </exception>
    public SshKeyFilePassword(string? password = null, Encoding? encoding = null)
    {
        if (string.IsNullOrWhiteSpace(password))
            return;
        if (encoding is not null)
            _encoding = encoding;

        var byteCount = _encoding.GetByteCount(password);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(byteCount, MaxPasswordBytes, nameof(password));

        // Encode into a stack-allocated temp buffer to avoid a heap copy
        Span<byte> temp = stackalloc byte[byteCount];
        _encoding.GetBytes(password, temp);
        WriteToBuffer(temp);
        CryptographicOperations.ZeroMemory(temp);
    }

    // ── Public API ──────────────────────────────────────────────────────

    /// <summary>
    /// Gets a value indicating whether the buffer contains a passphrase.
    /// </summary>
    [MemberNotNullWhen(true, nameof(WrittenSpan))]
    public bool IsValid
    {
        get
        {
            ThrowIfDisposed();
            return _writtenCount > 0;
        }
    }

    /// <summary>
    /// Gets the number of passphrase bytes currently stored.
    /// </summary>
    public int Length
    {
        get
        {
            ThrowIfDisposed();
            return _writtenCount;
        }
    }

    /// <summary>
    /// Gets a read-only span over the stored passphrase bytes.
    /// This is the <b>primary</b> way to consume the passphrase without heap allocation.
    /// </summary>
    /// <exception cref="ObjectDisposedException"/>
    public ReadOnlySpan<byte> WrittenSpan
    {
        get
        {
            ThrowIfDisposed();
            return _buffer.AsSpan(0, _writtenCount);
        }
    }

    /// <summary>
    /// Decodes the stored passphrase into the caller-provided character buffer.
    /// The caller should wipe <paramref name="destination"/> after use.
    /// </summary>
    /// <param name="destination">Target buffer (ideally <c>stackalloc</c>).</param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="ObjectDisposedException"/>
    /// <exception cref="ArgumentException">Thrown when <paramref name="destination"/> is too small.</exception>
    public int GetChars(Span<char> destination)
    {
        ThrowIfDisposed();
        if (_writtenCount == 0) return 0;
        return _encoding.GetChars(_buffer.AsSpan(0, _writtenCount), destination);
    }

    /// <summary>
    /// Returns the maximum number of characters that <see cref="GetChars"/> could write
    /// for the currently stored passphrase. Useful for sizing a <c>stackalloc</c> buffer.
    /// </summary>
    public int GetMaxCharCount()
    {
        ThrowIfDisposed();
        return _encoding.GetMaxCharCount(_writtenCount);
    }

    /// <summary>
    /// Replaces the stored passphrase with the UTF-8 encoding of <paramref name="password"/>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"/>
    /// <exception cref="ObjectDisposedException"/>
    public void Set(string password, Encoding? encoding = null)
    {
        ThrowIfDisposed();
        var enc = encoding ?? _encoding;
        var byteCount = enc.GetByteCount(password);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(byteCount, MaxPasswordBytes, nameof(password));

        Span<byte> temp = stackalloc byte[byteCount];
        enc.GetBytes(password, temp);
        Set(temp, enc);
        CryptographicOperations.ZeroMemory(temp);
    }

    /// <summary>
    /// Replaces the stored passphrase with the given raw bytes.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"/>
    /// <exception cref="ObjectDisposedException"/>
    public void Set(ReadOnlySpan<byte> password, Encoding? encoding = null)
    {
        ThrowIfDisposed();
        SecureClearBuffer();
        _encoding = encoding ?? _encoding;
        WriteToBuffer(password);
    }

    /// <inheritdoc cref="Set(ReadOnlySpan{byte},Encoding?)"/>
    public void Set(ReadOnlyMemory<byte> password, Encoding? encoding = null)
    {
        Set(password.Span, encoding);
    }

    /// <summary>
    /// Securely wipes the passphrase buffer without disposing the instance,
    /// allowing it to be reused with <see cref="Set(ReadOnlySpan{byte},Encoding?)"/>.
    /// </summary>
    /// <exception cref="ObjectDisposedException"/>
    public void Clear()
    {
        ThrowIfDisposed();
        SecureClearBuffer();
        OnPropertyChanged(nameof(IsValid));
        OnPropertyChanged(nameof(Length));
    }

    // ── INotifyPropertyChanged ──────────────────────────────────────────

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    // ── IDisposable ─────────────────────────────────────────────────────

    /// <summary>
    /// Securely wipes the internal buffer and marks the instance as disposed.
    /// Subsequent calls are no-ops.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        SecureClearBuffer();
    }

    // ── Private helpers ─────────────────────────────────────────────────

    private void WriteToBuffer(ReadOnlySpan<byte> data)
    {
        ThrowIfDisposed();
        ArgumentOutOfRangeException.ThrowIfGreaterThan(
            _writtenCount + data.Length, MaxPasswordBytes, nameof(data));

        data.CopyTo(_buffer.AsSpan(_writtenCount));
        _writtenCount += data.Length;

        OnPropertyChanged(nameof(IsValid));
        OnPropertyChanged(nameof(Length));
    }

    private void SecureClearBuffer()
    {
        CryptographicOperations.ZeroMemory(_buffer);
        _writtenCount = 0;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    
    /// <summary>
    /// Converts the stored password bytes to a string on the heap.
    /// The resulting string cannot be wiped and will live until GC collection.
    /// </summary>
    public string GetPasswordString()
    {
        Span<char> chars = stackalloc char[GetMaxCharCount()];
        var written = GetChars(chars);
        var result = new string(chars[..written]);
        chars.Clear();
        return result;
    }
}