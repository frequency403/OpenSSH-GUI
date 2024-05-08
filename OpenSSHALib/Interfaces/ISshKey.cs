using Renci.SshNet;
using Renci.SshNet.Security;
using SshNet.Keygen;

namespace OpenSSHALib.Interfaces;

public interface ISshKey
{
    public SshKeyFormat Format { get; }
    public string AbsoluteFilePath { get; }
    public string KeyTypeString { get; }
    public string Filename { get; }
    public string Comment { get; }
    public bool IsPublicKey { get; }
    public ISshKeyType KeyType { get; }
    public string Fingerprint { get; }
    public Task<string> ExportKeyAsync(SshKeyFormat format = SshKeyFormat.OpenSSH);
    public string ExportKey(SshKeyFormat format = SshKeyFormat.OpenSSH);
    public IPrivateKeySource GetRenciKeyType();
    void DeleteKey();
}