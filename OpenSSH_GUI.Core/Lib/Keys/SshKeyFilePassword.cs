using System.Buffers;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Text;
using OpenSSH_GUI.Core.Lib.Misc;
using ReactiveUI;
using ReactiveUI.Avalonia;
using ReactiveUI.SourceGenerators;
using SshNet.Keygen;
using SshNet.Keygen.SshKeyEncryption;

namespace OpenSSH_GUI.Core.Lib.Keys;

/// <summary>
///     A reactive, disposable container for an SSH key passphrase stored as raw bytes.
///     Acts as a two-state machine: <c>Empty</c> ↔ <c>HasPassword</c>.
///     All observable properties (<see cref="IsValid" /> fire
///     <see cref="System.ComponentModel.INotifyPropertyChanged.PropertyChanged" />
///     on every state transition triggered by <see cref="Set" /> or <see cref="Clear" />.
/// </summary>
public sealed partial record SshKeyFilePassword : ReactiveRecord, IDisposable
{
    private readonly ReactiveBufferWriter<byte> _bufferWriter = new(ushort.MaxValue);
    private readonly CompositeDisposable _disposables = new();
    private Encoding _encoding = Encoding.UTF8;

    /// <summary>
    ///     Gets a value indicating whether the buffer contains at least one byte.
    /// </summary>
    [Reactive(SetModifier = AccessModifier.Private)]
    private bool _isValid;

    /// <summary>
    ///     Initialises the state machine and wires all derived properties
    ///     to the internal mutation subject.
    /// </summary>
    public SshKeyFilePassword()
    {
        _bufferWriter.WhenAnyValue(vm => vm.WrittenCount)
            .ObserveOn(AvaloniaScheduler.Instance)
            .Select(e => e != 0)
            .Subscribe(eval =>
            {
                this.RaisePropertyChanging(nameof(WrittenSpan));
                IsValid = eval;
                this.RaisePropertyChanged(nameof(WrittenSpan));
            })
            .DisposeWith(_disposables);
    }

    // ── Span accessor ────────────────────────────────────────────────────

    /// <summary>
    ///     Gets a read-only span over the stored passphrase bytes.
    ///     This is the primary way to consume the passphrase without heap allocation.
    ///     <para>
    ///         <see cref="System.ComponentModel.INotifyPropertyChanged.PropertyChanged" />
    ///         is raised for this member on every <see cref="Set" /> or <see cref="Clear" /> call.
    ///     </para>
    /// </summary>
    public ReadOnlySpan<byte> WrittenSpan => _bufferWriter.WrittenSpan;

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

    // ── State transitions ────────────────────────────────────────────────

    /// <summary>
    ///     Replaces the stored passphrase with the given raw bytes and transitions
    ///     the instance to the <c>HasPassword</c> state (or stays there on overwrite).
    ///     Notifies all reactive observers after the write is complete.
    /// </summary>
    /// <param name="password">Raw passphrase bytes to store.</param>
    /// <param name="encoding">
    ///     Optional encoding override used by <see cref="GetPasswordString" />.
    ///     Defaults to the previously configured encoding.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException" />
    /// <exception cref="ObjectDisposedException" />
    public void Set(ReadOnlySpan<byte> password, Encoding? encoding = null)
    {
        _bufferWriter.Clear();
        _encoding = encoding ?? _encoding;
        _bufferWriter.Write(password);
    }

    /// <summary>
    ///     Securely wipes the passphrase buffer and transitions the instance
    ///     to the <c>Empty</c> state without disposing it, allowing reuse.
    ///     Notifies all reactive observers after the wipe.
    /// </summary>
    /// <exception cref="ObjectDisposedException" />
    public void Clear()
    {
        _bufferWriter.Clear();
    }

    /// <summary>
    ///     Decodes the stored passphrase to a managed <see cref="string" /> on the heap.
    ///     The resulting string <b>cannot</b> be securely wiped and will persist until GC collection.
    /// </summary>
    public string GetPasswordString()
    {
        if(!IsValid) 
            return string.Empty;
        
        Span<char> chars = stackalloc char[_encoding.GetMaxCharCount(_bufferWriter.WrittenCount)];
        var written = _encoding.GetChars(_bufferWriter.WrittenSpan, chars);
        var result = new string(chars[..written]);
        chars.Clear();
        return result;
    }

    public ISshKeyEncryption ToSshKeyEncryption(SshKeyFormat? format = null)
    {
        return this is { IsValid: true } keyPassword
            ? new SshKeyEncryptionAes256(
                keyPassword.GetPasswordString(),
                format is SshKeyFormat.PuTTYv3 ? new PuttyV3Encryption() : null)
            : SshKeyGenerateInfo.DefaultSshKeyEncryption;
    }
}