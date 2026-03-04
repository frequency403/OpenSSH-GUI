using System.Diagnostics;
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
    private FileInfo? fileInfo;
    private PrivateKeyFile? privateKeyFile;
    public bool IsInitialized => privateKeyFile != null && fileInfo is { Exists: true };
    
    public IReadOnlyCollection<HostAlgorithm> HostKeyAlgorithms => privateKeyFile?.HostKeyAlgorithms ?? throw new SshPassPhraseNullOrEmptyException();
    public Key Key => privateKeyFile?.Key ?? throw new SshPassPhraseNullOrEmptyException();
    public Certificate? Certificate => privateKeyFile?.Certificate ?? throw new SshPassPhraseNullOrEmptyException();

    private (string keySize, string fingerprintAlgorithm, string fingerprint, string comment, string keyType)
        extractKeyInformation()
    {
        var processInformation = new ProcessStartInfo
        {
            FileName = "ssh-keygen",
            Arguments = $"-lf {fileInfo.FullName}",
            CreateNoWindow = true,
            WorkingDirectory = fileInfo.DirectoryName,
            UseShellExecute = false,
            RedirectStandardOutput = true
        };
        var process = Process.Start(processInformation);
        var splitted = process.StandardOutput.ReadToEnd().TrimEnd('\r', '\n').Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var pingerprintSplit = splitted[1].Split(':');
        return (splitted[0], pingerprintSplit[0], pingerprintSplit[1], splitted[2], splitted[3]);
    }

    private int keySizeField = 0;
    public int KeySize => privateKeyFile?.Key.KeyLength ?? keySizeField;

    private SshKeyHashAlgorithmName hashAlgorithmNameField = SshKeyHashAlgorithmName.SHA256;

    public SshKeyHashAlgorithmName HashAlgorithmName =>
        Enum.TryParse<SshKeyHashAlgorithmName>(privateKeyFile?.HostKeyAlgorithms.FirstOrDefault()?.Name,
            out var enumValue)
            ? enumValue
            : hashAlgorithmNameField;
    
    private string fingerPrintField = string.Empty;
    public string FingerprintString => privateKeyFile?.Fingerprint(HashAlgorithmName) ?? fingerPrintField;
    
    private string commentField = string.Empty;
    public string Comment => privateKeyFile?.Key.Comment ?? commentField;
    
    private string keyTypeField = string.Empty;
    public string KeyType => privateKeyFile?.HostKeyAlgorithms.FirstOrDefault()?.Name ?? keyTypeField;
    
    public void Load(string filePath, string? passPhrase = null)
    {
        try
        {
            fileInfo = new FileInfo(filePath);
            privateKeyFile = new PrivateKeyFile(fileInfo.FullName, passPhrase);
        }
        catch (SshPassPhraseNullOrEmptyException)
        {
            Logger.LogWarning("Missing Password for keyfile {filePath}", filePath);
            var info = extractKeyInformation();

            keySizeField = int.Parse(info.keySize);
            hashAlgorithmNameField = Enum.Parse<SshKeyHashAlgorithmName>(info.fingerprintAlgorithm);
            fingerPrintField = info.fingerprint;
            commentField = info.comment;
            keyTypeField = info.keyType;
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to Initialize {className}", nameof(SshKeyFile));
            throw;
        }
    }
    
    public string ToPublic() => privateKeyFile?.ToPublic() ?? throw new SshPassPhraseNullOrEmptyException();
    public string ToPublic(SshKeyFormat format) => privateKeyFile?.ToPublic(format) ?? throw new SshPassPhraseNullOrEmptyException();
    
    public string Fingerprint() => privateKeyFile?.Fingerprint() ?? throw new SshPassPhraseNullOrEmptyException();
    public string Fingerprint(SshKeyHashAlgorithmName hashAlgorithmName) => privateKeyFile?.Fingerprint(hashAlgorithmName) ?? throw new SshPassPhraseNullOrEmptyException();
    
    public string ToOpenSshFormat() => privateKeyFile?.ToOpenSshFormat() ?? throw  new SshPassPhraseNullOrEmptyException();
    public string ToOpenSshFormat(string passphrase) => privateKeyFile?.ToOpenSshFormat(passphrase) ?? throw  new SshPassPhraseNullOrEmptyException();
    public string ToOpenSshFormat(ISshKeyEncryption keyEncryption) => privateKeyFile?.ToOpenSshFormat(keyEncryption) ?? throw  new SshPassPhraseNullOrEmptyException();
    
    public string ToPuttyFormat() => privateKeyFile?.ToPuttyFormat() ?? throw new SshPassPhraseNullOrEmptyException();
    public string ToPuttyFormat(string passphrase) => privateKeyFile?.ToPuttyFormat(passphrase) ?? throw new SshPassPhraseNullOrEmptyException();
    public string ToPuttyFormat(string passphrase, SshKeyFormat keyFormat) => privateKeyFile?.ToPuttyFormat(passphrase, keyFormat) ?? throw new SshPassPhraseNullOrEmptyException();
    public string ToPuttyFormat(ISshKeyEncryption keyEncryption, SshKeyFormat keyFormat) => privateKeyFile?.ToPuttyFormat(keyEncryption, keyFormat) ?? throw new SshPassPhraseNullOrEmptyException();
    public string ToPuttyFormat(SshKeyFormat keyFormat) => privateKeyFile?.ToPuttyFormat(keyFormat) ?? throw new SshPassPhraseNullOrEmptyException();
    
    public string ToOpenSshPublicFormat() => privateKeyFile?.ToOpenSshPublicFormat() ?? throw new SshPassPhraseNullOrEmptyException();
    public string ToPuttyPublicFormat => privateKeyFile?.ToPuttyPublicFormat() ?? throw new SshPassPhraseNullOrEmptyException();


    public bool SetPassword(ReadOnlySpan<byte> password)
    {
        try
        {
            Load(fileInfo.FullName, Encoding.UTF8.GetString(password));
            return true;
        }
        catch (SshPassPhraseNullOrEmptyException)
        {
            Logger.LogWarning("Missing Password for keyfile {filePath}", fileInfo.FullName);
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