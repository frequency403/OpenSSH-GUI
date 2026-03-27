using System.Buffers;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace OpenSSH_GUI.Core.Lib.Keys;

/// <summary>
///     A reactive, disposable container for an SSH key passphrase stored as raw bytes.
///     Acts as a two-state machine: <c>Empty</c> ↔ <c>HasPassword</c>.
///     All observable properties (<see cref="IsValid"/>, <see cref="Length"/>,
///     <see cref="WrittenCount"/>, <see cref="WrittenSpan"/>) fire
///     <see cref="System.ComponentModel.INotifyPropertyChanged.PropertyChanged"/>
///     on every state transition triggered by <see cref="Set"/> or <see cref="Clear"/>.
/// </summary>
public sealed partial record SshKeyFilePassword : ReactiveRecord, IDisposable
{
    // ── Private state ────────────────────────────────────────────────────

    private readonly ArrayBufferWriter<byte> _bufferWriter = new();

    /// <summary>Fires Unit.Default on every buffer mutation (Set / Clear).</summary>
    private readonly Subject<Unit> _bufferMutated = new();

    private readonly CompositeDisposable _disposables = new();

    // ── Encoding ─────────────────────────────────────────────────────────

    private Encoding Encoding
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = Encoding.UTF8;

    // ── Derived observable properties ────────────────────────────────────

    /// <summary>
    ///     Gets a value indicating whether the buffer contains at least one byte.
    /// </summary>
    [ObservableAsProperty(ReadOnly = true)]
    private bool _isValid;

    /// <summary>
    ///     Gets the number of passphrase bytes currently stored in the buffer.
    /// </summary>
    [ObservableAsProperty(ReadOnly = true)]
    private int _length;

    /// <summary>
    ///     Gets the number of bytes written to the buffer.
    ///     Alias of <see cref="Length"/> kept for interface compatibility.
    /// </summary>
    [ObservableAsProperty(ReadOnly = true)]
    private int _writtenCount;

    // ── Constructor ──────────────────────────────────────────────────────

    /// <summary>
    ///     Initialises the state machine and wires all derived properties
    ///     to the internal mutation subject.
    /// </summary>
    public SshKeyFilePassword(ILogger<SshKeyFilePassword> logger)
    {
        // Single shared stream: emits once immediately (StartWith) so that
        // all ObservableAsProperty helpers have a synchronous initial value,
        // then re-emits on every Set / Clear call.
        var bufferState = _bufferMutated
            .StartWith(Unit.Default)
            .Publish()
            .RefCount();

        _isValidHelper = bufferState
            .Select(_ => _bufferWriter.WrittenCount > 0)
            .Do(v => logger.LogDebug("IsValid changing to {Value}", v))
            .ToProperty(this, x => x.IsValid)
            .DisposeWith(_disposables);

        _lengthHelper = bufferState
            .Select(_ => _bufferWriter.WrittenCount)
            .Do(v => logger.LogDebug("Length changing to {Value}", v))
            .ToProperty(this, x => x.Length)
            .DisposeWith(_disposables);

        _writtenCountHelper = bufferState
            .Select(_ => _bufferWriter.WrittenCount)
            .Do(v => logger.LogDebug("WrittenCount changing to {Value}", v))
            .ToProperty(this, x => x.WrittenCount)
            .DisposeWith(_disposables);
        
        _bufferMutated
            .Subscribe(_ => this.RaisePropertyChanged(nameof(WrittenSpan)))
            .DisposeWith(_disposables);

        _disposables.Add(_bufferMutated);
    }

    // ── Span accessor ────────────────────────────────────────────────────

    /// <summary>
    ///     Gets a read-only span over the stored passphrase bytes.
    ///     This is the primary way to consume the passphrase without heap allocation.
    ///     <para>
    ///         <see cref="System.ComponentModel.INotifyPropertyChanged.PropertyChanged"/>
    ///         is raised for this member on every <see cref="Set"/> or <see cref="Clear"/> call.
    ///     </para>
    /// </summary>
    public ReadOnlySpan<byte> WrittenSpan => _bufferWriter.WrittenSpan;

    // ── State transitions ────────────────────────────────────────────────

    /// <summary>
    ///     Replaces the stored passphrase with the given raw bytes and transitions
    ///     the instance to the <c>HasPassword</c> state (or stays there on overwrite).
    ///     Notifies all reactive observers after the write is complete.
    /// </summary>
    /// <param name="password">Raw passphrase bytes to store.</param>
    /// <param name="encoding">
    ///     Optional encoding override used by <see cref="GetChars"/> and
    ///     <see cref="GetMaxCharCount"/>. Defaults to the previously configured encoding.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException"/>
    /// <exception cref="ObjectDisposedException"/>
    public void Set(ReadOnlySpan<byte> password, Encoding? encoding = null)
    {
        _bufferWriter.Clear();
        _bufferWriter.ResetWrittenCount();
        Encoding = encoding ?? Encoding;
        _bufferWriter.Write(password);
        _bufferMutated.OnNext(Unit.Default);
    }

    /// <summary>
    ///     Securely wipes the passphrase buffer and transitions the instance
    ///     to the <c>Empty</c> state without disposing it, allowing reuse.
    ///     Notifies all reactive observers after the wipe.
    /// </summary>
    /// <exception cref="ObjectDisposedException"/>
    public void Clear()
    {
        _bufferWriter.Clear();
        _bufferWriter.ResetWrittenCount();
        _bufferMutated.OnNext(Unit.Default);
    }

    // ── IDisposable ──────────────────────────────────────────────────────

    /// <summary>
    ///     Securely wipes the internal buffer, completes the mutation subject,
    ///     and disposes all reactive subscriptions.
    ///     Subsequent calls are no-ops.
    /// </summary>
    public void Dispose()
    {
        _bufferWriter.Clear();
        _disposables.Dispose(); // completes bufferMutated and all ToProperty helpers
    }

    // ── Passphrase accessors ─────────────────────────────────────────────

    /// <summary>
    ///     Decodes the stored passphrase into the caller-provided character buffer.
    ///     The caller is responsible for wiping <paramref name="destination"/> after use.
    /// </summary>
    /// <param name="destination">
    ///     Target buffer; ideally allocated with <c>stackalloc</c>.
    ///     Size it using <see cref="GetMaxCharCount"/>.
    /// </param>
    /// <returns>The number of characters written.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="destination"/> is too small.</exception>
    public int GetChars(Span<char> destination) =>
        Encoding.GetChars(_bufferWriter.WrittenSpan, destination);

    /// <summary>
    ///     Returns the maximum number of characters that <see cref="GetChars"/> could write
    ///     for the currently stored passphrase. Use this to size a <c>stackalloc</c> buffer.
    /// </summary>
    public int GetMaxCharCount() => Encoding.GetMaxCharCount(WrittenCount);

    /// <summary>
    ///     Decodes the stored passphrase to a managed <see cref="string"/> on the heap.
    ///     The resulting string <b>cannot</b> be securely wiped and will persist until GC collection.
    ///     Prefer <see cref="GetChars"/> for security-sensitive consumers.
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