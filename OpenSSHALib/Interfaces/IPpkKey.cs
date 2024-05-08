using OpenSSHALib.Enums;
using OpenSSHALib.Models;
using SshNet.Keygen;

namespace OpenSSHALib.Interfaces;

public interface IPpkKey : ISshKey
{
    EncryptionType EncryptionType { get; }
    string PublicKeyString { get; }
    string PrivateKeyString { get; }
    string PrivateMAC { get; }
    ISshPublicKey? ConvertToOpenSshKey(out string errorMessage, bool temp = false);
    public Task<string> ExportKeyAsync(bool publicKey = true, SshKeyFormat format = SshKeyFormat.OpenSSH);
    public string ExportKey(bool publicKey = true, SshKeyFormat format = SshKeyFormat.OpenSSH);
}
