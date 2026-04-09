using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Lib.AuthorizedKeys;
using ReactiveUI;
using ReactiveUI.Avalonia;
using ReactiveUI.SourceGenerators;
using Renci.SshNet;
using Renci.SshNet.Common;
using Renci.SshNet.Security;
using SshNet.Keygen;
using SshNet.Keygen.Extensions;
using SshNet.Keygen.SshKeyEncryption;

namespace OpenSSH_GUI.Core.Lib.Keys;

/// <summary>
///     Represents an SSH key file used in the OpenSSH GUI application, encapsulating properties
///     and functionality for managing SSH keys.
/// </summary>
// REFACTOR: Change to OAPH where appropriate
public sealed partial record SshKeyFile : ReactiveRecord, IDisposable, IAsyncDisposable
{
    private readonly CompositeDisposable _disposables = new();

    /// <summary>
    ///     A logger instance used for logging events and diagnostic information
    ///     related to the operations and state within the <see cref="SshKeyFile" /> class.
    /// </summary>
    private readonly ILogger<SshKeyFile> _logger;

    /// <summary>
    ///     Gets the absolute file path of the SSH key file, projected from <see cref="KeyFileInfo" />.
    /// </summary>
    [Reactive(SetModifier = AccessModifier.Private)]
    private string? _absoluteFilePath;

    /// <summary>
    ///     The basic key file information extracted by <c>ssh-keygen</c> or the file itself in case of a PuTTy key.
    /// </summary>
    [Reactive] private BasicSshKeyFileInformation _basicSshKeyFileInformation;

    /// <summary>
    ///     Holds the comment embedded in the SSH key, derived from either the loaded
    ///     <see cref="PrivateKeyFile" /> or the <see cref="BasicSshKeyFileInformation" />.
    /// </summary>
    [Reactive(SetModifier = AccessModifier.Private)]
    private string _comment = SshKeyGenerateInfo.DefaultSshKeyComment;

    [Reactive(SetModifier = AccessModifier.Private)]
    private bool _fileChangesAllowed;

    /// <summary>
    ///     Gets the file name (without directory) of the SSH key file, projected from <see cref="KeyFileInfo" />.
    /// </summary>
    [Reactive(SetModifier = AccessModifier.Private)]
    private string? _fileName;

    /// <summary>
    ///     Holds the raw fingerprint hash of the SSH key, derived from either the loaded
    ///     <see cref="PrivateKeyFile" /> or the <see cref="BasicSshKeyFileInformation" />.
    /// </summary>
    [Reactive(SetModifier = AccessModifier.Private)]
    private string _fingerprint = string.Empty;

    /// <summary>
    ///     Contains a human-readable fingerprint representation (the base64 portion after the
    ///     algorithm prefix), derived from either the loaded <see cref="PrivateKeyFile" /> or
    ///     the <see cref="BasicSshKeyFileInformation" />.
    /// </summary>
    [Reactive(SetModifier = AccessModifier.Private)]
    private string _fingerprintString = string.Empty;

    /// <summary>
    ///     Gets the current on-disk format of the SSH key file (e.g. OpenSSH or PuTTY),
    ///     projected from <see cref="KeyFileInfo" />.
    /// </summary>
    [Reactive(SetModifier = AccessModifier.Private)]
    private SshKeyFormat? _format;

    /// <summary>
    ///     Indicates the hash algorithm (e.g. SHA256, MD5) used for the key's fingerprint,
    ///     resolved from the host key algorithms of the loaded <see cref="PrivateKeyFile" />
    ///     or from <see cref="BasicSshKeyFileInformation" />.
    /// </summary>
    [Reactive(SetModifier = AccessModifier.Private)]
    private SshKeyHashAlgorithmName _hashAlgorithmName = SshKeyHashAlgorithmName.SHA256;

    /// <summary>
    ///     Evaluates to <see langword="true" /> when both a valid <see cref="PrivateKeyFile" />
    ///     is loaded and the associated <see cref="KeyFileInfo" /> exists on disk.
    /// </summary>
    [Reactive(SetModifier = AccessModifier.Private)]
    private bool _isInitialized;

    /// <summary>
    ///     Evaluates to <see langword="true" /> when the current key file format is not
    ///     <see cref="SshKeyFormat.OpenSSH" />, indicating a PuTTY-compatible key format.
    /// </summary>
    [Reactive(SetModifier = AccessModifier.Private)]
    private bool _isPuttyKey;

    /// <summary>
    ///     Holds metadata information about the associated SSH key file, such as file path, name,
    ///     format, and available formats for conversion. Provides access to details about the
    ///     primary key file and related files, such as public key files.
    /// </summary>
    [Reactive] private SshKeyFileInformation? _keyFileInfo;

    /// <summary>
    ///     Provides access to the collection of associated key files (e.g. private and public)
    ///     for the current SSH key, projected from <see cref="KeyFileInfo" />.
    ///     Returns an empty array when no files are associated.
    /// </summary>
    [Reactive(SetModifier = AccessModifier.Private)]
    private FileInfo[] _keyFiles = [];

    /// <summary>
    ///     Represents the cryptographic algorithm of the key (e.g. RSA, ECDSA, ED25519),
    ///     resolved from the loaded <see cref="PrivateKeyFile" /> or <see cref="BasicSshKeyFileInformation" />.
    /// </summary>
    [Reactive(SetModifier = AccessModifier.Private)]
    private SshKeyType _keyType = SshKeyType.RSA;

    /// <summary>
    ///     Indicates whether the associated SSH key file requires a password to access.
    /// </summary>
    [Reactive(SetModifier = AccessModifier.Private)]
    private bool _needsPassword;

    /// <summary>
    ///     Represents a password container for an SSH key file, encapsulating related
    ///     password properties and operations while supporting password validation.
    /// </summary>
    [Reactive(SetModifier = AccessModifier.Private)]
    private SshKeyFilePassword _password = new();

    /// <summary>
    ///     Represents the underlying private key file used to interact with SSH-related
    ///     operations, including authentication and cryptographic functions.
    /// </summary>
    [Reactive] private PrivateKeyFile? _privateKeyFile;

    /// <summary>
    ///     Initializes a new instance of <see cref="SshKeyFile" />, wires up all reactive
    ///     observable property pipelines.
    /// </summary>
    /// <param name="logger">
    ///     An <see cref="ILogger{SshKeyFile}" /> used for diagnostic output throughout the
    ///     lifetime of this instance.
    /// </param>
    public SshKeyFile(ILogger<SshKeyFile> logger)
    {
        _logger = logger;
        Password = new SshKeyFilePassword();
        this.WhenAnyValue(x => x.PrivateKeyFile, x => x.BasicSshKeyFileInformation)
            .ObserveOn(AvaloniaScheduler.Instance)
            .Subscribe(tuple =>
            {
                NeedsPassword = tuple.Item1 == null;
                try
                {
                    Fingerprint = tuple.Item1?.FingerprintHash() ?? tuple.Item2.FingerPrint;
                }
                catch
                {
                    Fingerprint = tuple.Item2.FingerPrint;
                }

                try
                {
                    FingerprintString = tuple.Item1?.Fingerprint(SshKeyHashAlgorithmName.SHA256)
                        .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Skip(1).FirstOrDefault()
                        ?.Split(':').Skip(1).FirstOrDefault() ?? tuple.Item2.FingerPrint;
                }
                catch
                {
                    FingerprintString = tuple.Item2.FingerPrint;
                }

                try
                {
                    Comment = tuple.Item1?.Key.Comment ?? tuple.Item2.Comment;
                }
                catch
                {
                    Comment = tuple.Item2.Comment;
                }

                if (tuple.Item1 is { } pk)
                {
                    KeyType = pk.Key switch
                    {
                        EcdsaKey => SshKeyType.ECDSA,
                        ED25519Key => SshKeyType.ED25519,
                        _ => SshKeyType.RSA
                    };
                    HashAlgorithmName = Enum.TryParse<SshKeyHashAlgorithmName>(
                        pk.HostKeyAlgorithms.FirstOrDefault()?.Name ?? string.Empty,
                        out var enumValue)
                        ? enumValue
                        : tuple.Item2.HashAlgorithmName;
                }

                KeyType = tuple.Item2.KeyType;
                HashAlgorithmName = tuple.Item2.HashAlgorithmName;
            }).DisposeWith(_disposables);

        this.WhenAnyValue(
                vm => vm.NeedsPassword,
                vm => vm.Password,
                vm => vm.KeyFileInfo,
                (needsPassword, password, keyFileInfo) =>
                    keyFileInfo is { KeyFileSource.ProvidedByConfig: false } &&
                    (!needsPassword || password.IsValid))
            .ObserveOn(AvaloniaScheduler.Instance)
            .Subscribe(result => FileChangesAllowed = result)
            .DisposeWith(_disposables);

        this.WhenAnyValue(vm => vm.PrivateKeyFile, vm => vm.KeyFileInfo)
            .ObserveOn(AvaloniaScheduler.Instance)
            .Subscribe(tuple =>
            {
                IsInitialized = tuple.Item1 is not null && tuple.Item2 is { Exists: true };
                IsPuttyKey = tuple.Item2?.CurrentFormat is not SshKeyFormat.OpenSSH;
                KeyFiles = tuple.Item2 is not null ? tuple.Item2.Files : [];
                AbsoluteFilePath = tuple.Item2?.FullFileName;
                FileName = tuple.Item2?.FileName;
                Format = tuple.Item2?.CurrentFormat;
                try
                {
                    if (tuple.Item2 is not null)
                        BasicSshKeyFileInformation = BasicSshKeyFileInformation.FromKeyFileInfo(tuple.Item2);
                }
                catch (FileNotFoundException)
                {
                }
                catch (Exception e)
                {
                    logger.LogInformation(e, "Failed to extract key information");
                }
            })
            .DisposeWith(_disposables);
    }

    /// <summary>
    ///     Represents the authorized key associated with an SSH key file.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the SSH key file is not initialized.
    /// </exception>
    public AuthorizedKey AuthorizedKey
    {
        get
        {
            if (PrivateKeyFile is { } privateKeyFile)
                return AuthorizedKey.Parse(privateKeyFile.ToOpenSshPublicFormat());
            throw new InvalidOperationException("SshKeyFile not initialized.");
        }
    }

    /// <summary>
    ///     Asynchronously releases the unmanaged resources used by the SshKeyFile instance
    ///     and optionally releases the managed resources.
    /// </summary>
    public ValueTask DisposeAsync()
    {
        _disposables.Dispose();
        return ValueTask.CompletedTask;
    }

    /// <summary>
    ///     Releases the unmanaged resources used by the <see cref="SshKeyFile" /> instance
    ///     and optionally releases the managed resources.
    /// </summary>
    public void Dispose()
    {
        _disposables.Dispose();
    }

    /// <summary>
    ///     Implicit conversion to the underlying <see cref="Renci.SshNet.PrivateKeyFile" />.
    /// </summary>
    public static implicit operator PrivateKeyFile?(SshKeyFile sshKeyFile)
    {
        return sshKeyFile.PrivateKeyFile;
    }

    /// <summary>
    ///     Resets the state of the current SSH key file instance, clearing any previously set password,
    ///     and reinitializing the associated private key file to its initial state.
    ///     If the associated key file requires a password to decrypt but no password is set,
    ///     the method updates the state to indicate that a password is needed and attempts to
    ///     extract key metadata for further operations. Logs errors and rethrows exceptions
    ///     in case of unexpected failures during the reset process.
    /// </summary>
    public void Reset()
    {
        try
        {
            PrivateKeyFile?.Dispose();
            PrivateKeyFile = null;
            Password.Clear();
            PrivateKeyFile = new PrivateKeyFile(KeyFileInfo!.FullFileName);
        }
        catch (SshPassPhraseNullOrEmptyException)
        {
            if (KeyFileInfo is not null)
                BasicSshKeyFileInformation = BasicSshKeyFileInformation.FromKeyFileInfo(KeyFileInfo);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to Initialize {className}", nameof(SshKeyFile));
            throw;
        }

        _logger.LogInformation("Reset {className} successfully", KeyFileInfo?.FileName ?? string.Empty);
    }

    public void Load(SshKeyFileSource source)
    {
        try
        {
            KeyFileInfo = new SshKeyFileInformation(source);
            PrivateKeyFile = Password.IsValid
                ? new PrivateKeyFile(KeyFileInfo.FullFileName, Password.GetPasswordString())
                : new PrivateKeyFile(KeyFileInfo.FullFileName);
        }
        catch (SshPassPhraseNullOrEmptyException passPhraseNullOrEmptyException)
        {
            _logger.LogInformation(passPhraseNullOrEmptyException, "Missing Password for keyfile {filePath}",
                source.AbsolutePath);
            if (KeyFileInfo is not null)
                BasicSshKeyFileInformation = BasicSshKeyFileInformation.FromKeyFileInfo(KeyFileInfo);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to Initialize {className}", nameof(SshKeyFile));
            throw;
        }
    }

    /// <summary>
    ///     Loads an SSH key file from the specified file path and initializes it,
    ///     optionally using the provided passphrase for decryption.
    /// </summary>
    /// <param name="keyFileSource">The SSH key file source from with the file is to be loaded.</param>
    /// <param name="passPhrase">
    ///     An optional passphrase for the key file, used to unlock encrypted private keys.
    /// </param>
    public void Load(SshKeyFileSource keyFileSource, ReadOnlySpan<byte> passPhrase)
    {
        try
        {
            if (passPhrase is { Length: > 0 } pass)
                Password.Set(pass);
            Load(keyFileSource);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to Initialize {className}", nameof(SshKeyFile));
            throw;
        }
    }

    /// <summary>
    ///     Sets the password for the SSH key file by loading the key file using the provided password.
    /// </summary>
    /// <param name="password">A read-only memory block containing the password to initialize the SSH key file.</param>
    /// <returns>
    ///     A boolean value indicating whether the password was successfully set.
    /// </returns>
    public bool SetPassword(ReadOnlySpan<byte> password)
    {
        try
        {
            if (KeyFileInfo is not { Exists: true })
                throw new FileNotFoundException("SshKeyFile not found", KeyFileInfo?.FileName);
            Load(KeyFileInfo.KeyFileSource, password);
            return true;
        }
        catch (SshPassPhraseNullOrEmptyException)
        {
            _logger.LogWarning("Missing Password for keyfile {filePath}", KeyFileInfo?.FullFileName);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to Initialize {className}", nameof(SshKeyFile));
        }

        return false;
    }
}