using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reactive;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Lib.AuthorizedKeys;
using OpenSSH_GUI.Core.Services;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using Renci.SshNet;
using Renci.SshNet.Common;
using Renci.SshNet.Security;
using SshNet.Keygen;
using SshNet.Keygen.Extensions;
using SshNet.Keygen.SshKeyEncryption;

namespace OpenSSH_GUI.Core.Lib.Keys;

/// <summary>
/// Represents an SSH key file used in the OpenSSH GUI application, encapsulating properties
/// and functionality for managing SSH keys.
/// </summary>
public sealed partial class SshKeyFile : ReactiveObject, IDisposable, IAsyncDisposable
{
    /// <summary>
    /// A logger instance used for logging events and diagnostic information
    /// related to the operations and state within the <see cref="SshKeyFile"/> class.
    /// </summary>
    private readonly ILogger<SshKeyFile> _logger;

    /// <summary>
    /// Manages operations related to transformation and management of SSH key files.
    /// Provides functionality to change the file format of SSH keys on disk
    /// and serves as the core service dependency for <see cref="SshKeyFile"/>.
    /// </summary>
    private readonly SshKeyManager _sshKeyManager;

    /// <summary>
    /// Stores the comment associated with the SSH key file.
    /// This field is primarily used internally for extracting or storing
    /// metadata related to the SSH key during file operations or processing.
    /// Default value is an empty string.
    /// </summary>
    private string _commentField = string.Empty;

    /// <summary>
    /// Holds metadata information about the associated SSH key file, such as file path, name,
    /// format, and available formats for conversion. Provides access to details about the
    /// primary key file and related files, such as public key files.
    /// </summary>
    /// <remarks>
    /// The source generator creates a public property named <c>KeyFileInfo</c> from this field.
    /// The name avoids collision with <see cref="System.IO.FileInfo"/>.
    /// </remarks>
    [Reactive]
    private SshKeyFileInformation? _keyFileInfo;

    /// <summary>
    /// Stores the fingerprint information of the SSH key.
    /// </summary>
    private string _fingerPrintField = string.Empty;

    /// <summary>
    /// Represents the default hash algorithm name used for calculating SSH key fingerprints.
    /// This field is initialized to the SHA256 algorithm by default and can be updated
    /// during key information extraction or other relevant operations. It is used as a fallback
    /// in cases where the private key's host key algorithm name cannot be determined.
    /// </summary>
    private SshKeyHashAlgorithmName _hashAlgorithmNameField = SshKeyHashAlgorithmName.SHA256;

    /// <summary>
    /// Represents the internal field used to store the key type in string representation.
    /// This field is primarily utilized for parsing and determining the appropriate
    /// <see cref="SshKeyType"/> when the associated SSH key metadata is loaded or updated.
    /// </summary>
    private string _keyTypeField = string.Empty;

    /// <summary>
    /// Represents the underlying private key file used to interact with SSH-related
    /// operations, including authentication and cryptographic functions.
    /// </summary>
    /// <remarks>
    /// The source generator creates a public property named <c>PrivateKeyFile</c> from this field.
    /// </remarks>
    [Reactive]
    private PrivateKeyFile? _privateKeyFile;

    // --- ObservableAsProperty backing fields ---
    // The source generator creates public read-only properties and
    // corresponding _xxxHelper fields from each of these.

    [ObservableAsProperty]
    private PrivateKeyFile? _privateKeySource;

    [ObservableAsProperty]
    private string _fingerprint = string.Empty;

    [ObservableAsProperty]
    private string _fingerprintString = string.Empty;

    [ObservableAsProperty]
    private string _comment = string.Empty;

    [ObservableAsProperty]
    private SshKeyType _keyType = SshKeyType.RSA;

    [ObservableAsProperty]
    private SshKeyHashAlgorithmName _hashAlgorithmName = SshKeyHashAlgorithmName.SHA256;

    [ObservableAsProperty]
    private bool _isInitialized;

    [ObservableAsProperty]
    private bool _isPuttyKey;

    /// <summary>
    /// Represents a file-based SSH key with fully encapsulated functionalities for managing,
    /// manipulating, and interacting with the key. This class provides support for operations
    /// such as key format conversion, password management, and key metadata retrieval.
    /// Implements <see cref="ReactiveObject"/> for reactive binding capabilities and
    /// both <see cref="IDisposable"/> and <see cref="IAsyncDisposable"/> for lifecycle management.
    /// </summary>
    public SshKeyFile(ILogger<SshKeyFile> logger, SshKeyManager sshKeyManager)
    {
        _sshKeyManager = sshKeyManager;
        _logger = logger;
        ChangeFormatOfKeyFile = ReactiveCommand.CreateFromTask<SshKeyFormat>(ChangeFormatOnDisk);

        // Wire up all computed ObservableAsPropertyHelper properties.

        _privateKeySourceHelper = this.WhenAnyValue(x => x.PrivateKeyFile)
            .ToProperty(this, nameof(PrivateKeySource));

        _fingerprintHelper = this.WhenAnyValue(x => x.PrivateKeyFile)
            .Select(pk => pk?.FingerprintHash() ?? _fingerPrintField)
            .ToProperty(this, nameof(Fingerprint));

        _fingerprintStringHelper = this.WhenAnyValue(x => x.PrivateKeyFile)
            .Select(pk => pk?.Fingerprint(SshKeyHashAlgorithmName.SHA256)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Skip(1).FirstOrDefault()
                ?.Split(':').Skip(1).FirstOrDefault() ?? _fingerPrintField)
            .ToProperty(this, nameof(FingerprintString));

        _commentHelper = this.WhenAnyValue(x => x.PrivateKeyFile)
            .Select(pk => pk?.Key.Comment ?? _commentField)
            .ToProperty(this, nameof(Comment));

        _keyTypeHelper = this.WhenAnyValue(x => x.PrivateKeyFile)
            .Select(pk =>
            {
                if (pk is not null)
                    return pk.Key switch
                    {
                        EcdsaKey => SshKeyType.ECDSA,
                        ED25519Key => SshKeyType.ED25519,
                        _ => SshKeyType.RSA
                    };
                return Enum.TryParse<SshKeyType>(_keyTypeField, true, out var enumValue)
                    ? enumValue
                    : SshKeyType.RSA;
            })
            .ToProperty(this, nameof(KeyType));

        _hashAlgorithmNameHelper = this.WhenAnyValue(x => x.PrivateKeyFile)
            .Select(pk =>
                Enum.TryParse<SshKeyHashAlgorithmName>(pk?.HostKeyAlgorithms.FirstOrDefault()?.Name, out var enumValue)
                    ? enumValue
                    : _hashAlgorithmNameField)
            .ToProperty(this, nameof(HashAlgorithmName));

        _isInitializedHelper = this.WhenAnyValue(x => x.PrivateKeyFile, x => x.KeyFileInfo)
            .Select(t => t.Item1 is not null && t.Item2 is { Exists: true })
            .ToProperty(this, nameof(IsInitialized));

        _isPuttyKeyHelper = this.WhenAnyValue(x => x.KeyFileInfo)
            .Select(fi => fi?.CurrentFormat is not SshKeyFormat.OpenSSH)
            .ToProperty(this, nameof(IsPuttyKey));
    }

    /// <summary>
    /// Provides access to the collection of associated key files for the current SSH key.
    /// The key files typically include the private and public key files that are associated
    /// with the key being managed. This property relies on the underlying <see cref="SshKeyFileInformation"/>
    /// instance to determine and fetch the file information.
    /// Returns an enumeration of <see cref="FileInfo"/> objects, representing the files associated
    /// with the SSH key. If no files are linked to the key, an empty enumeration is returned.
    /// </summary>
    internal IEnumerable<FileInfo> KeyFiles => KeyFileInfo?.Files ?? [];

    /// <summary>
    /// Represents the authorized key associated with an SSH key file.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the SSH key file is not initialized.
    /// </exception>
    public AuthorizedKey AuthorizedKey
    {
        get
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Not initialized.");
            return AuthorizedKey.Parse(PrivateKeyFile!.ToOpenSshPublicFormat());
        }
    }

    /// <summary>
    /// Indicates whether the associated SSH key file requires a password to access.
    /// </summary>
    public bool NeedsPassword
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>
    /// Represents a password container for an SSH key file, encapsulating related
    /// password properties and operations while supporting password validation.
    /// </summary>
    public SshKeyFilePassword Password { get; } = new();

    /// <summary>
    /// Gets the absolute file path of the SSH key file.
    /// </summary>
    public string? AbsoluteFilePath => KeyFileInfo?.FullName;

    /// <summary>
    /// Gets the name of the SSH key file.
    /// </summary>
    public string? FileName => KeyFileInfo?.Name;

    /// <summary>
    /// Gets the current format of the SSH key file.
    /// </summary>
    public SshKeyFormat? Format => KeyFileInfo?.CurrentFormat;

    /// <summary>
    /// Gets the list of available SSH key formats to which the current key can be converted.
    /// </summary>
    public IEnumerable<SshKeyFormat>? AvailableFormatsForConversion => KeyFileInfo?.AvailableFormatsForConversion;

    /// <summary>
    /// Gets the default format to which the key file can be converted.
    /// </summary>
    public SshKeyFormat? DefaultConversionFormat => KeyFileInfo?.DefaultConversionFormat;

    /// <summary>
    /// A reactive command that allows changing the format of an SSH key file on disk.
    /// </summary>
    public ReactiveCommand<SshKeyFormat, Unit> ChangeFormatOfKeyFile { get; }

    /// <summary>
    /// Event triggered when the SSH key file is successfully deleted.
    /// </summary>
    public EventHandler? GotDeleted { get; set; } = delegate { };

    /// <summary>
    /// Asynchronously releases the unmanaged resources used by the SshKeyFile instance
    /// and optionally releases the managed resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (PrivateKeyFile is IAsyncDisposable privateKeyFileAsyncDisposable)
            await privateKeyFileAsyncDisposable.DisposeAsync();
        else
            PrivateKeyFile?.Dispose();
    }

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="SshKeyFile"/> instance
    /// and optionally releases the managed resources.
    /// </summary>
    public void Dispose()
    {
        PrivateKeyFile?.Dispose();
    }

    /// <summary>
    /// Implicit conversion to the underlying <see cref="Renci.SshNet.PrivateKeyFile"/>.
    /// </summary>
    public static implicit operator PrivateKeyFile?(SshKeyFile sshKeyFile)
    {
        return sshKeyFile.PrivateKeyFile;
    }

    /// <summary>
    /// Extracts detailed information about the SSH key file, such as its fingerprint,
    /// hash algorithm, comment, and key type, using the <c>ssh-keygen</c> command-line tool.
    /// </summary>
    /// <exception cref="FileNotFoundException">
    /// Thrown if the SSH key file does not exist.
    /// </exception>
    private async ValueTask ExtractKeyInformation()
    {
        if (KeyFileInfo is not { Exists: true })
            throw new FileNotFoundException();
        var processInformation = new ProcessStartInfo
        {
            FileName = "ssh-keygen",
            Arguments = $"-lf {KeyFileInfo.FullName}",
            CreateNoWindow = true,
            WorkingDirectory = KeyFileInfo.DirectoryName,
            UseShellExecute = false,
            RedirectStandardOutput = true
        };

        if (Process.Start(processInformation) is { } process)
        {
            var splitted = (await process.StandardOutput.ReadToEndAsync()).TrimEnd('\r', '\n').Split(' ',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var pingerprintSplit = splitted[1].Split(':');

            _hashAlgorithmNameField = Enum.Parse<SshKeyHashAlgorithmName>(pingerprintSplit[0]);
            _fingerPrintField = pingerprintSplit[1];
            _commentField = splitted[2];
            _keyTypeField = splitted[3];

            _logger.LogInformation("Extracted Key Information from {filePath}: \"{joinedString}\"",
                KeyFileInfo.FullName, string.Join(" ", splitted));
        }
    }

    /// <summary>
    /// Loads an SSH key file from the specified file path and initializes it,
    /// optionally using the provided passphrase for decryption.
    /// </summary>
    /// <param name="filePath">The full file path of the SSH key file to be loaded.</param>
    /// <param name="passPhrase">
    /// An optional passphrase for the key file, used to unlock encrypted private keys.
    /// </param>
    public async ValueTask Load(string filePath, ReadOnlyMemory<byte>? passPhrase = null)
    {
        try
        {
            KeyFileInfo = new SshKeyFileInformation(filePath);
            if (passPhrase is { Length: > 0 } pass)
                Password.Set(pass);
            PrivateKeyFile = Password.IsValid
                ? new PrivateKeyFile(KeyFileInfo.FullName, Password.GetPasswordString())
                : new PrivateKeyFile(KeyFileInfo.FullName);
        }
        catch (SshPassPhraseNullOrEmptyException)
        {
            _logger.LogWarning("Missing Password for keyfile {filePath}", filePath);
            NeedsPassword = true;
            await ExtractKeyInformation();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to Initialize {className}", nameof(SshKeyFile));
            throw;
        }
    }

    /// <summary>
    /// Changes the format of the SSH key file on disk to the specified format.
    /// </summary>
    /// <param name="newFormat">The new format to apply to the SSH key file.</param>
    /// <param name="token">A cancellation token to observe while waiting for the operation to complete.</param>
    private Task ChangeFormatOnDisk(SshKeyFormat newFormat, CancellationToken token = default)
    {
        if (!IsInitialized) throw new InvalidOperationException("Not initialized.");
        _logger.LogInformation("Changing format of keyfile {filePath} to {newFormat}",
            KeyFileInfo!.FullName, newFormat);
        return _sshKeyManager.ChangeFormatOfKeyAsync(this, newFormat, token);
    }

    /// <summary>
    /// Sets the password for the SSH key file by loading the key file using the provided password.
    /// </summary>
    /// <param name="password">A read-only memory block containing the password to initialize the SSH key file.</param>
    /// <returns>
    /// A boolean value indicating whether the password was successfully set.
    /// </returns>
    public async ValueTask<bool> SetPassword(ReadOnlyMemory<byte> password)
    {
        try
        {
            if (KeyFileInfo is not { Exists: true })
                throw new FileNotFoundException("SshKeyFile not found", KeyFileInfo?.Name);
            await Load(KeyFileInfo.FullName, password);
            NeedsPassword = false;
            return true;
        }
        catch (SshPassPhraseNullOrEmptyException)
        {
            NeedsPassword = true;
            _logger.LogWarning("Missing Password for keyfile {filePath}", KeyFileInfo?.FullName);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to Initialize {className}", nameof(SshKeyFile));
        }

        return false;
    }

    /// <summary>
    /// Deletes all files associated with this SSH key. If all deletions complete successfully,
    /// the <see cref="GotDeleted"/> event will be triggered.
    /// </summary>
    /// <returns>
    /// A boolean indicating whether all files were successfully deleted.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the SSH key file is not initialized before calling this method.
    /// </exception>
    public bool Delete()
    {
        if (!IsInitialized)
            throw new InvalidOperationException("Not initialized.");

        var allSucceeded = true;
        foreach (var file in KeyFileInfo!.Files)
            try
            {
                file.Delete();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to delete {FilePath}", file.FullName);
                allSucceeded = false;
            }

        if (allSucceeded)
            GotDeleted(this, EventArgs.Empty);
        return allSucceeded;
    }

    /// <summary>
    /// Changes the filename of the SSH key file on disk to the specified new filename.
    /// </summary>
    /// <param name="newFilename">The new filename to assign to the SSH key file.</param>
    public void ChangeFilenameOnDisk(string newFilename)
    {
        try
        {
            foreach (var file in KeyFileInfo?.Files ?? [])
            {
                var newFileNameWithMatchingExtension = Path.ChangeExtension(newFilename,
                    string.IsNullOrEmpty(file.Extension) ? null : file.Extension);
                var destination = Path.Combine(
                    file.DirectoryName ?? SshConfigFilesExtension.GetBaseSshPath(),
                    newFileNameWithMatchingExtension);
                if (File.Exists(destination))
                    throw new InvalidOperationException($"File {destination} already exists");
                file.MoveTo(destination);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to change filename of {className}", nameof(SshKeyFile));
            throw;
        }
    }
}