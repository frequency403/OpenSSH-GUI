using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reactive;
using System.Text;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Interfaces.AuthorizedKeys;
using OpenSSH_GUI.Core.Interfaces.Services;
using OpenSSH_GUI.Core.Lib.AuthorizedKeys;
using ReactiveUI;
using Renci.SshNet;
using Renci.SshNet.Common;
using Renci.SshNet.Security;
using SshNet.Keygen;
using SshNet.Keygen.Extensions;
using SshNet.Keygen.SshKeyEncryption;

namespace OpenSSH_GUI.Core.Lib.Keys;

public sealed class SshKeyFile : ReactiveObject, IDisposable, IAsyncDisposable
{
    private readonly ILogger<SshKeyFile> _logger;
    private readonly ISshKeyManager _sshKeyManager;


    private string _commentField = string.Empty;
    private SshKeyFileInformation? _fileInfo;

    private string _fingerPrintField = string.Empty;

    private SshKeyHashAlgorithmName _hashAlgorithmNameField = SshKeyHashAlgorithmName.SHA256;

    private int _keySizeField;

    private string _keyTypeField = string.Empty;
    private PrivateKeyFile? _privateKeyFile;

    public SshKeyFile(ILogger<SshKeyFile> logger, ISshKeyManager sshKeyManager)
    {
        _sshKeyManager = sshKeyManager;
        _logger = logger;
        ChangeFormatOfKeyFile = ReactiveCommand.CreateFromTask<SshKeyFormat>(ChangeFormatOnDisk);
    }

    public IPrivateKeySource PrivateKeySource => _privateKeyFile ?? throw new InvalidOperationException("Not initialized.");

    internal IEnumerable<FileInfo> KeyFiles => _fileInfo?.Files ?? [];

    [MemberNotNullWhen(true, nameof(_fileInfo), nameof(_privateKeyFile))]
    public bool IsInitialized => _privateKeyFile is not null && _fileInfo is { Exists: true };

    public AuthorizedKey AuthorizedKey
    {
        get
        {
            if(!IsInitialized)
                throw new InvalidOperationException("Not initialized.");
            return AuthorizedKey.Parse(_privateKeyFile.ToOpenSshPublicFormat());
        }
    }

    public bool IsPuttyKey => _fileInfo?.CurrentFormat is not SshKeyFormat.OpenSSH;

    public bool NeedsPassword
    {
        get; 
        set => this.RaiseAndSetIfChanged(ref field, value);
    }
    
    public SshKeyFilePassword Password { get; } = new ();

    public string Fingerprint => _privateKeyFile?.FingerprintHash() ?? _fingerPrintField;
    
    public IReadOnlyCollection<HostAlgorithm>? HostKeyAlgorithms => _privateKeyFile?.HostKeyAlgorithms;
    public Key? Key => _privateKeyFile?.Key;
    public string? AbsoluteFilePath => _fileInfo?.FullName;
    public string? FileName => _fileInfo?.Name;
    public SshKeyFormat? Format => _fileInfo?.CurrentFormat;
    public IEnumerable<SshKeyFormat>? AvailableFormatsForConversion => _fileInfo?.AvailableFormatsForConversion;
    public SshKeyFormat? DefaultConversionFormat => _fileInfo?.DefaultConversionFormat;
    public ReactiveCommand<SshKeyFormat, Unit> ChangeFormatOfKeyFile { get; }
    public Certificate? Certificate => _privateKeyFile?.Certificate;
    public int KeySize => _privateKeyFile?.Key.KeyLength ?? _keySizeField;

    public SshKeyHashAlgorithmName HashAlgorithmName =>
        Enum.TryParse<SshKeyHashAlgorithmName>(_privateKeyFile?.HostKeyAlgorithms.FirstOrDefault()?.Name,
            out var enumValue)
            ? enumValue
            : _hashAlgorithmNameField;

    public string FingerprintString => _privateKeyFile?.Fingerprint(SshKeyHashAlgorithmName.SHA256)
        .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Skip(1).FirstOrDefault()
        ?.Split(':').Skip(1).FirstOrDefault() ?? _fingerPrintField;

    public string Comment => _privateKeyFile?.Key.Comment ?? _commentField;

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

    public EventHandler? GotDeleted { get; set; } = delegate { };

    public async ValueTask DisposeAsync()
    {
        if (_privateKeyFile is IAsyncDisposable privateKeyFileAsyncDisposable)
            await privateKeyFileAsyncDisposable.DisposeAsync();
        else
            _privateKeyFile?.Dispose();
    }

    public void Dispose()
    {
        _privateKeyFile?.Dispose();
    }

    public static implicit operator PrivateKeyFile?(SshKeyFile sshKeyFile)
    {
        return sshKeyFile._privateKeyFile;
    }

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

            _keySizeField = int.Parse(splitted[0]);
            _hashAlgorithmNameField = Enum.Parse<SshKeyHashAlgorithmName>(pingerprintSplit[0]);
            _fingerPrintField = pingerprintSplit[1];
            _commentField = splitted[2];
            _keyTypeField = splitted[3];
        }
    }

    public async ValueTask Load(string filePath, ReadOnlyMemory<byte>? passPhrase = null)
    {
        try
        {
            _fileInfo = new SshKeyFileInformation(filePath);
            if(passPhrase is { Length: > 0 } pass)
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

    private Task ChangeFormatOnDisk(SshKeyFormat newFormat, CancellationToken token = default)
    {
        if(!IsInitialized) throw new InvalidOperationException("Not initialized.");
        _logger.LogInformation("Changing format of keyfile {filePath} to {newFormat}", _fileInfo.FullName, newFormat);
        return _sshKeyManager.ChangeFormatOfKeyAsync(this, newFormat, token);
    }

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
}