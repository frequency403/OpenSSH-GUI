using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Renci.SshNet;
using Renci.SshNet.Common;
using Renci.SshNet.Security;
using SshNet.Keygen;
using SshNet.Keygen.Extensions;
using SshNet.Keygen.SshKeyEncryption;

namespace OpenSSH_GUI.Core.Lib.Keys;

public sealed record SshKeyFile(ILogger<SshKeyFile> Logger) : IDisposable, IAsyncDisposable
{
    private string _commentField = string.Empty;
    private FileInfo? _fileInfo;

    private string _fingerPrintField = string.Empty;

    private SshKeyHashAlgorithmName _hashAlgorithmNameField = SshKeyHashAlgorithmName.SHA256;

    private int _keySizeField;

    private string _keyTypeField = string.Empty;
    private PrivateKeyFile? _privateKeyFile;

    public IPrivateKeySource? PrivateKeySource => _privateKeyFile;

    [MemberNotNullWhen(true, nameof(_fileInfo), nameof(_privateKeyFile))]
    public bool IsInitialized => _privateKeyFile != null && _fileInfo is { Exists: true };

    public bool IsPuttyKey => SshKeyFormat is not SshKeyFormat.OpenSSH;

    public bool NeedsPassword { get; set; }

    [MemberNotNullWhen(true, nameof(Password))]
    public bool HasPassword => Password is not null && !NeedsPassword;

    public ReadOnlyMemory<byte>? Password { get; set; }

    public IReadOnlyCollection<HostAlgorithm> HostKeyAlgorithms =>
        _privateKeyFile?.HostKeyAlgorithms ?? throw new SshPassPhraseNullOrEmptyException();

    public Key Key => _privateKeyFile?.Key ?? throw new SshPassPhraseNullOrEmptyException();
    public Certificate? Certificate => _privateKeyFile?.Certificate;
    public string AbsoluteFilePath => _fileInfo?.FullName ?? string.Empty;
    public string FileName => _fileInfo?.Name ?? string.Empty;

    public SshKeyFormat SshKeyFormat => _fileInfo?.Extension switch
    {
        { } extension when extension.EndsWith("ppk") => SshKeyFormat.PuTTYv3,
        _ => SshKeyFormat.OpenSSH
    };

    public int KeySize => _privateKeyFile?.Key.KeyLength ?? _keySizeField;

    public SshKeyHashAlgorithmName HashAlgorithmName =>
        Enum.TryParse<SshKeyHashAlgorithmName>(_privateKeyFile?.HostKeyAlgorithms.FirstOrDefault()?.Name,
            out var enumValue)
            ? enumValue
            : _hashAlgorithmNameField;

    public string FingerprintString => _privateKeyFile?.Fingerprint(HashAlgorithmName) ?? _fingerPrintField;
    public string Comment => _privateKeyFile?.Key.Comment ?? _commentField;
    public string KeyType => _privateKeyFile?.HostKeyAlgorithms.FirstOrDefault()?.Name ?? _keyTypeField;

    public string ToPuttyPublicFormat =>
        _privateKeyFile?.ToPuttyPublicFormat() ?? throw new SshPassPhraseNullOrEmptyException();

    public async ValueTask DisposeAsync()
    {
        if (_privateKeyFile is IAsyncDisposable privateKeyFileAsyncDisposable)
            await privateKeyFileAsyncDisposable.DisposeAsync();
        else if (_privateKeyFile != null)
            _privateKeyFile.Dispose();
    }


    public void Dispose()
    {
        _privateKeyFile?.Dispose();
    }

    public static implicit operator PrivateKeyFile?(SshKeyFile sshKeyFile)
    {
        return sshKeyFile._privateKeyFile;
    }

    internal static async ValueTask<SshKeyFile> FromFile(string filePath)
    {
        var keyFile = new SshKeyFile(NullLogger<SshKeyFile>.Instance);
        await keyFile.Load(filePath);
        return keyFile;
    }

    internal static async ValueTask<SshKeyFile> FromFile(string filePath, ReadOnlyMemory<byte> password)
    {
        var keyFile = new SshKeyFile(NullLogger<SshKeyFile>.Instance);
        await keyFile.Load(filePath, password);
        return keyFile;
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
            _fileInfo = new FileInfo(filePath);
            _privateKeyFile = passPhrase is not { } memory
                ? new PrivateKeyFile(_fileInfo.FullName)
                : new PrivateKeyFile(_fileInfo.FullName, Encoding.UTF8.GetString(memory.Span));
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

    public string ToPublic()
    {
        return _privateKeyFile?.ToPublic() ?? throw new SshPassPhraseNullOrEmptyException();
    }

    public string ToPublic(SshKeyFormat format)
    {
        return _privateKeyFile?.ToPublic(format) ?? throw new SshPassPhraseNullOrEmptyException();
    }

    public string Fingerprint()
    {
        return _privateKeyFile?.Fingerprint() ?? throw new SshPassPhraseNullOrEmptyException();
    }

    public string Fingerprint(SshKeyHashAlgorithmName hashAlgorithmName)
    {
        return _privateKeyFile?.Fingerprint(hashAlgorithmName) ?? throw new SshPassPhraseNullOrEmptyException();
    }

    public string? ToOpenSshFormat()
    {
        if (NeedsPassword)
            throw new SshPassPhraseNullOrEmptyException();
        if (HasPassword && IsInitialized)
            return _privateKeyFile.ToOpenSshFormat(Encoding.UTF8.GetString(Password.Value.Span));
        return IsInitialized ? _privateKeyFile.ToOpenSshFormat() : null;
    }

    public string ToOpenSshFormat(ISshKeyEncryption keyEncryption)
    {
        return _privateKeyFile?.ToOpenSshFormat(keyEncryption) ?? throw new SshPassPhraseNullOrEmptyException();
    }

    public string ToPuttyFormat()
    {
        return _privateKeyFile?.ToPuttyFormat() ?? throw new SshPassPhraseNullOrEmptyException();
    }

    public string ToPuttyFormat(string passphrase)
    {
        return _privateKeyFile?.ToPuttyFormat(passphrase) ?? throw new SshPassPhraseNullOrEmptyException();
    }

    public string ToPuttyFormat(string passphrase, SshKeyFormat keyFormat)
    {
        return _privateKeyFile?.ToPuttyFormat(passphrase, keyFormat) ?? throw new SshPassPhraseNullOrEmptyException();
    }

    public string ToPuttyFormat(ISshKeyEncryption keyEncryption, SshKeyFormat keyFormat)
    {
        return _privateKeyFile?.ToPuttyFormat(keyEncryption, keyFormat) ??
               throw new SshPassPhraseNullOrEmptyException();
    }

    public string ToPuttyFormat(SshKeyFormat keyFormat)
    {
        return _privateKeyFile?.ToPuttyFormat(keyFormat) ?? throw new SshPassPhraseNullOrEmptyException();
    }

    public string ToOpenSshPublicFormat()
    {
        return _privateKeyFile?.ToOpenSshPublicFormat() ?? throw new SshPassPhraseNullOrEmptyException();
    }


    public async ValueTask<bool> SetPassword(ReadOnlyMemory<byte> password)
    {
        try
        {
            if (_fileInfo is not { Exists: true })
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

    public ValueTask<bool> Delete()
    {
        try
        {
            if (!IsInitialized) return ValueTask.FromResult(false);
            var filesToDelete = new[] { AbsoluteFilePath }
                .Concat(Directory.EnumerateFiles(Path.GetDirectoryName(AbsoluteFilePath),
                    $"*{(SshKeyFormat is SshKeyFormat.OpenSSH ? ".pub" : string.Empty)}",
                    SearchOption.TopDirectoryOnly))
                .Where(e => string.Equals(Path.GetFileNameWithoutExtension(AbsoluteFilePath),
                    Path.GetFileNameWithoutExtension(e))).ToArray();
            Span<bool> span = stackalloc bool[filesToDelete.Length];
            for (var i = 0; i < span.Length; i++)
                try
                {
                    File.Delete(filesToDelete[i]);
                    span[i] = true;
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Failed to delete {filePath}", filesToDelete[i]);
                    span[i] = false;
                }

            return ValueTask.FromResult(span.CountAny(true) == span.Length);
        }
        catch (Exception exception)
        {
            return ValueTask.FromException<bool>(exception);
        }
    }
}