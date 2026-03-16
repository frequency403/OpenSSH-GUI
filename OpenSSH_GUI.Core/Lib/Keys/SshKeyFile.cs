using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reactive;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Lib.AuthorizedKeys;
using OpenSSH_GUI.Core.Services;
using ReactiveUI;
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
public sealed class SshKeyFile : ReactiveObject, IDisposable, IAsyncDisposable
{
    /// <summary>
    /// A logger instance used for logging events and diagnostic information
    /// related to the operations and state within the <see cref="SshKeyFile"/> class.
    /// </summary>
    /// <remarks>
    /// This logger is utilized for various purposes, including logging errors,
    /// warnings, informational messages, and other activities such as tracking
    /// key file processing, password requirements, and key format changes.
    /// The primary aim is to provide appropriate logging for effective debugging
    /// and monitoring of program execution.
    /// </remarks>
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
    private SshKeyFileInformation? _fileInfo;

    /// <summary>
    /// Stores the fingerprint information of the SSH key.
    /// </summary>
    /// <remarks>
    /// The fingerprint is extracted from the SSH key file and used for identification purposes.
    /// It may represent the hash of the key in a specific format, depending on the key's properties
    /// or the hash algorithm.
    /// This field is intended to persist the hash value when the private key file is not initialized,
    /// ensuring fingerprint availability even when the key file cannot currently provide it dynamically.
    /// </remarks>
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
    /// This variable stores the parsed private key file which provides access to
    /// cryptographic details and algorithms associated with the key. It is integral
    /// for operations such as fingerprint generation, key format conversion, and
    /// public key derivation.
    /// </remarks>
    private PrivateKeyFile? _privateKeyFile;

    /// <summary>
    /// Represents a file-based SSH key with fully encapsulated functionalities for managing,
    /// manipulating, and interacting with the key. This class provides support for operations
    /// such as key format conversion, password management, and key metadata retrieval.
    /// Implements <see cref="ReactiveObject"/> for reactive binding capabilities and
    /// both <see cref="IDisposable"/> and <see cref="IAsyncDisposable"/> for lifecycle management.
    /// </summary>
    /// <remarks>
    /// This class is designed to work with SSH private keys and supports a variety of
    /// operations including loading a key from disk, changing its format, setting
    /// passwords, and more. It also provides extensive information about the SSH key,
    /// such as its type, formats available for conversion, and cryptographic metadata
    /// like its fingerprint.
    /// </remarks>
    public SshKeyFile(ILogger<SshKeyFile> logger, SshKeyManager sshKeyManager)
    {
        _sshKeyManager = sshKeyManager;
        _logger = logger;
        ChangeFormatOfKeyFile = ReactiveCommand.CreateFromTask<SshKeyFormat>(ChangeFormatOnDisk);
    }

    /// <summary>
    /// Represents the source of a private key used for SSH connectivity.
    /// </summary>
    /// <remarks>
    /// This property provides access to the underlying private key file used for authentication
    /// in SSH connections. It retrieves the private key file if it has been properly initialized;
    /// otherwise, an exception is thrown. The source is integral to SSH-based communication
    /// functionality, acting as the key material for establishing secure and authenticated sessions.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the private key file has not been initialized.
    /// </exception>
    /// <seealso cref="Renci.SshNet.PrivateKeyFile"/>
    public IPrivateKeySource PrivateKeySource =>
        _privateKeyFile ?? throw new InvalidOperationException("Not initialized.");

    /// <summary>
    /// Provides access to the collection of associated key files for the current SSH key.
    /// The key files typically include the private and public key files that are associated
    /// with the key being managed. This property relies on the underlying <see cref="SshKeyFileInformation"/>
    /// instance to determine and fetch the file information.
    /// Returns an enumeration of <see cref="FileInfo"/> objects, representing the files associated
    /// with the SSH key. If no files are linked to the key, an empty enumeration is returned.
    /// The files retrieved by this property can include:
    /// - The primary private key file.
    /// - The corresponding public key file, if available.
    /// This property is primarily used during operations such as key management, format conversions,
    /// and cleanup processes.
    /// </summary>
    internal IEnumerable<FileInfo> KeyFiles => _fileInfo?.Files ?? [];

    /// <summary>
    /// Indicates whether the SSH key file is fully initialized and ready for use.
    /// </summary>
    /// <remarks>
    /// The property returns true when the private key file and its associated metadata
    /// are successfully loaded and the key file exists on disk.
    /// </remarks>
    [MemberNotNullWhen(true, nameof(_fileInfo), nameof(_privateKeyFile))]
    public bool IsInitialized => _privateKeyFile is not null && _fileInfo is { Exists: true };

    /// <summary>
    /// Represents the authorized key associated with an SSH key file.
    /// </summary>
    /// <remarks>
    /// This property provides access to an <see cref="AuthorizedKey"/> instance derived from the SSH key file.
    /// It is required that the SSH key file is initialized prior to accessing this property; otherwise, an
    /// <see cref="InvalidOperationException"/> is thrown. The <see cref="AuthorizedKey"/> contains details such as
    /// the key type, fingerprint, comment, and deletion status.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the SSH key file is not initialized.
    /// </exception>
    public AuthorizedKey AuthorizedKey
    {
        get
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Not initialized.");
            return AuthorizedKey.Parse(_privateKeyFile.ToOpenSshPublicFormat());
        }
    }

    /// <summary>
    /// Indicates whether the SSH key file is in the PuTTY key format.
    /// </summary>
    /// <remarks>
    /// This property returns <c>true</c> if the current format of the key file is not OpenSSH
    /// (e.g., PuTTY format), and <c>false</c> otherwise.
    /// </remarks>
    public bool IsPuttyKey => _fileInfo?.CurrentFormat is not SshKeyFormat.OpenSSH;

    /// <summary>
    /// Indicates whether the associated SSH key file requires a password to access.
    /// </summary>
    /// <remarks>
    /// This property is used to determine if the SSH key file is password-protected.
    /// When set to <c>true</c>, authentication or decryption requires a password.
    /// </remarks>
    public bool NeedsPassword
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>
    /// Represents a password container for an SSH key file, encapsulating related
    /// password properties and operations while supporting password validation.
    /// </summary>
    /// <remarks>
    /// The <c>Password</c> property is designed to handle and manage the passphrase
    /// associated with an SSH key file. It facilitates secure operations by ensuring
    /// the passphrase is valid and may be used in contexts requiring password-protected
    /// key files, such as conversions or exports.
    /// </remarks>
    public SshKeyFilePassword Password { get; } = new();

    /// <summary>
    /// Represents the fingerprint of the SSH key associated with this instance.
    /// The fingerprint is a hashed representation of the SSH key, which can be
    /// used as a unique identifier to verify the key's authenticity.
    /// </summary>
    /// <remarks>
    /// If the private key is loaded and initialized, the fingerprint is
    /// derived using the <c>FingerprintHash</c> method. If the key is
    /// not initialized, it falls back to a pre-defined internal string value.
    /// </remarks>
    public string Fingerprint => _privateKeyFile?.FingerprintHash() ?? _fingerPrintField;

    /// <summary>
    /// Gets the absolute file path of the SSH key file.
    /// </summary>
    /// <remarks>
    /// This property provides the full path to the SSH key file as a string, if it has been properly initialized.
    /// Returns <c>null</c> if the file information is not available or the file path is inaccessible.
    /// </remarks>
    public string? AbsoluteFilePath => _fileInfo?.FullName;

    /// <summary>
    /// Gets the name of the SSH key file.
    /// </summary>
    /// <remarks>
    /// The property retrieves the name of the file from the underlying file information object. If
    /// the file information is not initialized, this property may return null.
    /// </remarks>
    public string? FileName => _fileInfo?.Name;

    /// <summary>
    /// Gets the current format of the SSH key file.
    /// </summary>
    /// <remarks>
    /// The format indicates the key file's compatibility and structure. Common formats
    /// include OpenSSH and PuTTY. The value of this property is derived from the associated
    /// file's extension or identified structure.
    /// </remarks>
    /// <returns>
    /// A <see cref="SshKeyFormat"/> value representing the current format, or null if the format
    /// cannot be determined.
    /// </returns>
    public SshKeyFormat? Format => _fileInfo?.CurrentFormat;

    /// <summary>
    /// Gets the list of available SSH key formats to which the current key can be converted.
    /// </summary>
    /// <remarks>
    /// This property provides a collection of <see cref="SshKeyFormat"/> values representing
    /// the formats supported for conversion, excluding the current format of the key.
    /// </remarks>
    public IEnumerable<SshKeyFormat>? AvailableFormatsForConversion => _fileInfo?.AvailableFormatsForConversion;

    /// <summary>
    /// Gets the default format to which the key file can be converted.
    /// If the OpenSSH format is available in the supported conversion formats,
    /// it is selected as the default; otherwise, the highest-ranked format
    /// from the supported conversion formats is chosen.
    /// </summary>
    /// <remarks>
    /// The value is determined based on the current `SshKeyFileInformation` and its
    /// `AvailableFormatsForConversion`. This property may return null if the file
    /// information has not been initialized or is unavailable.
    /// </remarks>
    public SshKeyFormat? DefaultConversionFormat => _fileInfo?.DefaultConversionFormat;

    /// <summary>
    /// A reactive command that allows changing the format of an SSH key file on disk.
    /// </summary>
    /// <remarks>
    /// This property represents a reactive command that executes a task to convert the format of
    /// an existing key file to a specified <see cref="SshKeyFormat"/>. It is primarily used
    /// for reformatting SSH key files between different formats such as OpenSSH, PuTTY, etc.
    /// </remarks>
    /// <value>
    /// The command requires a parameter of type <see cref="SshKeyFormat"/> to specify the
    /// target format for the key file transformation.
    /// </value>
    /// <example>
    /// Bind the command to a UI control where users can select the desired target format.
    /// </example>
    public ReactiveCommand<SshKeyFormat, Unit> ChangeFormatOfKeyFile { get; }

    /// <summary>
    /// Gets the name of the hash algorithm used for generating the fingerprint of
    /// the SSH key file. The name corresponds to a value in the <see cref="SshKeyHashAlgorithmName"/> enumeration.
    /// </summary>
    /// <remarks>
    /// This property determines which hash algorithm is used to compute the SSH key fingerprint.
    /// The value is derived from the HostKeyAlgorithms selected for the private key file,
    /// or defaults to a predefined hash algorithm if no specific algorithm is selected or parsed.
    /// </remarks>
    public SshKeyHashAlgorithmName HashAlgorithmName =>
        Enum.TryParse<SshKeyHashAlgorithmName>(_privateKeyFile?.HostKeyAlgorithms.FirstOrDefault()?.Name,
            out var enumValue)
            ? enumValue
            : _hashAlgorithmNameField;

    /// <summary>
    /// Gets the fingerprint of the SSH key file as a formatted string representation.
    /// </summary>
    /// <remarks>
    /// The fingerprint, derived using the specified hash algorithm (e.g., SHA256), provides
    /// a unique identifier for the SSH key. The value is processed by extracting specific parts
    /// of the computed fingerprint string and may exclude unnecessary segments
    /// depending on the format.
    /// </remarks>
    /// <value>
    /// A string representation of the SSH key's fingerprint, or an empty string if the key file is not initialized.
    /// </value>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the fingerprint computation depends on components that are not initialized.
    /// </exception>
    public string FingerprintString => _privateKeyFile?.Fingerprint(SshKeyHashAlgorithmName.SHA256)
        .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Skip(1).FirstOrDefault()
        ?.Split(':').Skip(1).FirstOrDefault() ?? _fingerPrintField;

    /// <summary>
    /// Gets the comment associated with the SSH private key file.
    /// </summary>
    /// <remarks>
    /// This property retrieves the comment embedded in the private key file, if available.
    /// The comment usually provides metadata about the key, such as its purpose or the user associated with it.
    /// If no comment exists in the key file, an empty string is returned.
    /// </remarks>
    public string Comment => _privateKeyFile?.Key.Comment ?? _commentField;

    /// <summary>
    /// Represents the type of an SSH key, such as RSA, ECDSA, or ED25519.
    /// </summary>
    /// <remarks>
    /// This property determines the algorithm associated with the SSH key based on the private key file.
    /// If the private key file is not available, it attempts to parse the previously stored key type.
    /// Defaults to RSA when no type information can be determined.
    /// </remarks>
    public SshKeyType KeyType
    {
        get
        {
            if (_privateKeyFile is not null)
                return _privateKeyFile.Key switch
                {
                    EcdsaKey => SshKeyType.ECDSA,
                    ED25519Key => SshKeyType.ED25519,
                    _ => SshKeyType.RSA
                };
            return Enum.TryParse<SshKeyType>(_keyTypeField, true, out var enumValue) ? enumValue : SshKeyType.RSA;
        }
    }

    /// <summary>
    /// Event triggered when the SSH key file is successfully deleted.
    /// </summary>
    /// <remarks>
    /// This event is raised after all associated files of the SSH key have been
    /// successfully removed from the disk. It allows subscribers to perform actions
    /// upon the deletion of the SSH key file, such as updating the UI or removing
    /// references to the deleted key.
    /// </remarks>
    public EventHandler? GotDeleted { get; set; } = delegate { };

    /// <summary>
    /// Asynchronously releases the unmanaged resources used by the SshKeyFile instance and optionally releases the managed resources.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    public async ValueTask DisposeAsync()
    {
        if (_privateKeyFile is IAsyncDisposable privateKeyFileAsyncDisposable)
            await privateKeyFileAsyncDisposable.DisposeAsync();
        else
            _privateKeyFile?.Dispose();
    }

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="SshKeyFile"/> instance
    /// and optionally releases the managed resources. This method disposes of any
    /// associated resources, such as private key files, to ensure proper cleanup
    /// and resource management.
    /// </summary>
    public void Dispose()
    {
        _privateKeyFile?.Dispose();
    }

    /// <summary>
    /// Defines a custom operator to perform a specific operation on the given operands.
    /// </summary>
    /// <param name="left">The left-hand operand of the operator.</param>
    /// <param name="right">The right-hand operand of the operator.</param>
    /// <returns>
    /// The result of the operation performed on the specified operands.
    /// </returns>
    public static implicit operator PrivateKeyFile?(SshKeyFile sshKeyFile)
    {
        return sshKeyFile._privateKeyFile;
    }

    /// <summary>
    /// Extracts detailed information about the SSH key file, such as its fingerprint,
    /// hash algorithm, comment, and key type, using the `ssh-keygen` command-line tool.
    /// </summary>
    /// <exception cref="FileNotFoundException">
    /// Thrown if the SSH key file does not exist.
    /// </exception>
    /// <returns>
    /// A task that represents the asynchronous operation of extracting key information.
    /// </returns>
    private async ValueTask ExtractKeyInformation()
    {
        if (_fileInfo is not { Exists: true })
            throw new FileNotFoundException();
        var processInformation = new ProcessStartInfo
        {
            FileName = "ssh-keygen",
            Arguments = $"-lf {_fileInfo.FullName}",
            CreateNoWindow = true,
            WorkingDirectory = _fileInfo.DirectoryName,
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
            
            _logger.LogInformation("Extracted Key Information from {filePath}: \"{joinedString}\"", _fileInfo.FullName, string.Join(" ", splitted));
        }
    }

    /// <summary>
    /// Loads an SSH key file from the specified file path and initializes it,
    /// optionally using the provided passphrase for decryption.
    /// </summary>
    /// <param name="filePath">
    /// The full file path of the SSH key file to be loaded.
    /// </param>
    /// <param name="passPhrase">
    /// An optional passphrase for the key file, used to unlock encrypted private keys.
    /// Defaults to null if the key file does not require a passphrase.
    /// </param>
    /// <returns>
    /// A <see cref="ValueTask"/> representing the asynchronous operation of loading the key file.
    /// </returns>
    /// <exception cref="FileNotFoundException">
    /// Thrown if the specified key file does not exist.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the key file fails to initialize due to invalid state.
    /// </exception>
    /// <exception cref="Exception">
    /// Thrown for other unexpected errors during the loading and initialization process.
    /// </exception>
    public async ValueTask Load(string filePath, ReadOnlyMemory<byte>? passPhrase = null)
    {
        try
        {
            _fileInfo = new SshKeyFileInformation(filePath);
            if (passPhrase is { Length: > 0 } pass)
                Password.Set(pass);
            _privateKeyFile = Password.IsValid
                ? new PrivateKeyFile(_fileInfo.FullName, Password.GetPasswordString())
                : new PrivateKeyFile(_fileInfo.FullName);
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
    /// <returns>A task that represents the asynchronous operation of changing the key file format.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the SSH key file is not initialized before calling this method.</exception>
    private Task ChangeFormatOnDisk(SshKeyFormat newFormat, CancellationToken token = default)
    {
        if (!IsInitialized) throw new InvalidOperationException("Not initialized.");
        _logger.LogInformation("Changing format of keyfile {filePath} to {newFormat}", _fileInfo.FullName, newFormat);
        return _sshKeyManager.ChangeFormatOfKeyAsync(this, newFormat, token);
    }

    /// <summary>
    /// Sets the password for the SSH key file by loading the key file using the provided password.
    /// </summary>
    /// <param name="password">A read-only memory block containing the password to initialize the SSH key file.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a boolean value
    /// indicating whether the password was successfully set.
    /// </returns>
    public async ValueTask<bool> SetPassword(ReadOnlyMemory<byte> password)
    {
        try
        {
            if (_fileInfo is not { Exists: true })
                throw new FileNotFoundException("SshKeyFile not found", _fileInfo?.Name);
            await Load(_fileInfo.FullName, password);
            NeedsPassword = false;
            return true;
        }
        catch (SshPassPhraseNullOrEmptyException)
        {
            NeedsPassword = true;
            _logger.LogWarning("Missing Password for keyfile {filePath}", _fileInfo?.FullName);
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
    /// If true, all deletions succeeded; if false, one or more deletion operations failed.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the SSH key file is not initialized before calling this method.
    /// </exception>
    public bool Delete()
    {
        if (!IsInitialized)
            throw new InvalidOperationException("Not initialized.");

        var allSucceeded = true;
        foreach (var file in _fileInfo.Files)
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
    }
}