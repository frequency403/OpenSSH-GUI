using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.Extensions.Logging;
using Renci.SshNet;
using Renci.SshNet.Common;
using Renci.SshNet.Security;
using SshNet.Keygen;
using SshNet.Keygen.Extensions;
using SshNet.Keygen.SshKeyEncryption;

namespace OpenSSH_GUI.Core.Lib.Misc;

public sealed record SshKeyFile(ILogger<SshKeyFile> Logger) : IDisposable, IAsyncDisposable
{
    private FileInfo? _fileInfo;
    private PrivateKeyFile? _privateKeyFile;
    
    [MemberNotNullWhen(true, nameof(_fileInfo), nameof(_privateKeyFile))]
    public bool IsInitialized => _privateKeyFile != null && _fileInfo is { Exists: true };

    public bool IsPuttyKey => SshKeyFormat is not SshKeyFormat.OpenSSH;
    
    public bool NeedsPassword { get; set; }
    
    [MemberNotNullWhen(true, nameof(Password))]
    public bool HasPassword => Password is not null && !NeedsPassword;
    public ReadOnlyMemory<byte>? Password { get; set; } = null;
    
    public IReadOnlyCollection<HostAlgorithm> HostKeyAlgorithms => _privateKeyFile?.HostKeyAlgorithms ?? throw new SshPassPhraseNullOrEmptyException();
    public Key Key => _privateKeyFile?.Key ?? throw new SshPassPhraseNullOrEmptyException();
    public Certificate? Certificate => _privateKeyFile?.Certificate;
    public string AbsoluteFilePath => _fileInfo?.FullName ?? string.Empty;
    public string FileName => _fileInfo?.Name ?? string.Empty;

    public SshKeyFormat SshKeyFormat => _fileInfo?.Extension switch
    {
        { } extension when extension.EndsWith("ppk") => SshKeyFormat.PuTTYv3,
        _ => SshKeyFormat.OpenSSH
    };

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
            var splitted = (await process.StandardOutput.ReadToEndAsync()).TrimEnd('\r', '\n').Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var pingerprintSplit = splitted[1].Split(':');
        
            _keySizeField = int.Parse(splitted[0]);
            _hashAlgorithmNameField = Enum.Parse<SshKeyHashAlgorithmName>(pingerprintSplit[0]);
            _fingerPrintField = pingerprintSplit[1];
            _commentField = splitted[2];
            _keyTypeField = splitted[3];
        }
    }

    private int _keySizeField;
    public int KeySize => _privateKeyFile?.Key.KeyLength ?? _keySizeField;

    private SshKeyHashAlgorithmName _hashAlgorithmNameField = SshKeyHashAlgorithmName.SHA256;

    public SshKeyHashAlgorithmName HashAlgorithmName =>
        Enum.TryParse<SshKeyHashAlgorithmName>(_privateKeyFile?.HostKeyAlgorithms.FirstOrDefault()?.Name,
            out var enumValue)
            ? enumValue
            : _hashAlgorithmNameField;
    
    private string _fingerPrintField = string.Empty;
    public string FingerprintString => _privateKeyFile?.Fingerprint(HashAlgorithmName) ?? _fingerPrintField;
    
    private string _commentField = string.Empty;
    public string Comment => _privateKeyFile?.Key.Comment ?? _commentField;
    
    private string _keyTypeField = string.Empty;
    public string KeyType => _privateKeyFile?.HostKeyAlgorithms.FirstOrDefault()?.Name ?? _keyTypeField;
    
    public async ValueTask Load(string filePath, ReadOnlyMemory<byte>? passPhrase = null)
    {
        try
        {
            _fileInfo = new FileInfo(filePath);
            _privateKeyFile = passPhrase is not { } memory ? new PrivateKeyFile(_fileInfo.FullName) : new PrivateKeyFile(_fileInfo.FullName, Encoding.UTF8.GetString(memory.Span));
            Password = passPhrase;
        }
        catch (SshPassPhraseNullOrEmptyException)
        {
            Logger.LogWarning("Missing Password for keyfile {filePath}", filePath);
            await ExtractKeyInformation();
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to Initialize {className}", nameof(SshKeyFile));
            throw;
        }
    }
    
    public string ToPublic() => _privateKeyFile?.ToPublic() ?? throw new SshPassPhraseNullOrEmptyException();
    public string ToPublic(SshKeyFormat format) => _privateKeyFile?.ToPublic(format) ?? throw new SshPassPhraseNullOrEmptyException();
    
    public string Fingerprint() => _privateKeyFile?.Fingerprint() ?? throw new SshPassPhraseNullOrEmptyException();
    public string Fingerprint(SshKeyHashAlgorithmName hashAlgorithmName) => _privateKeyFile?.Fingerprint(hashAlgorithmName) ?? throw new SshPassPhraseNullOrEmptyException();

    public string? ToOpenSshFormat()
    {
        if(NeedsPassword)
            throw new SshPassPhraseNullOrEmptyException();
        if (HasPassword && IsInitialized)
            return _privateKeyFile.ToOpenSshFormat(Encoding.UTF8.GetString(Password.Value.Span));
        return IsInitialized ? _privateKeyFile.ToOpenSshFormat() : null;
    }
    
    public string ToOpenSshFormat(ISshKeyEncryption keyEncryption) => _privateKeyFile?.ToOpenSshFormat(keyEncryption) ?? throw  new SshPassPhraseNullOrEmptyException();
    
    public string ToPuttyFormat() => _privateKeyFile?.ToPuttyFormat() ?? throw new SshPassPhraseNullOrEmptyException();
    public string ToPuttyFormat(string passphrase) => _privateKeyFile?.ToPuttyFormat(passphrase) ?? throw new SshPassPhraseNullOrEmptyException();
    public string ToPuttyFormat(string passphrase, SshKeyFormat keyFormat) => _privateKeyFile?.ToPuttyFormat(passphrase, keyFormat) ?? throw new SshPassPhraseNullOrEmptyException();
    public string ToPuttyFormat(ISshKeyEncryption keyEncryption, SshKeyFormat keyFormat) => _privateKeyFile?.ToPuttyFormat(keyEncryption, keyFormat) ?? throw new SshPassPhraseNullOrEmptyException();
    public string ToPuttyFormat(SshKeyFormat keyFormat) => _privateKeyFile?.ToPuttyFormat(keyFormat) ?? throw new SshPassPhraseNullOrEmptyException();
    
    public string ToOpenSshPublicFormat() => _privateKeyFile?.ToOpenSshPublicFormat() ?? throw new SshPassPhraseNullOrEmptyException();
    public string ToPuttyPublicFormat => _privateKeyFile?.ToPuttyPublicFormat() ?? throw new SshPassPhraseNullOrEmptyException();


    public async ValueTask<bool> SetPassword(ReadOnlyMemory<byte> password)
    {
        try
        {
            if(_fileInfo is not { Exists: true})
                throw new Exception("");
            await Load(_fileInfo.FullName, password);
            NeedsPassword = false;
            return true;
        }
        catch (SshPassPhraseNullOrEmptyException)
        {
            NeedsPassword = true;
            Logger.LogWarning("Missing Password for keyfile {filePath}", _fileInfo.FullName);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to Initialize {className}", nameof(SshKeyFile));
        }
        return false;
    }
    
    public void Dispose()
    {
        // TODO release managed resources here
    }

    public async ValueTask DisposeAsync()
    {
        // TODO release managed resources here
    }
}