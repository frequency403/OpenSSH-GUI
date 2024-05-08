using OpenSSHALib.Interfaces;
using Renci.SshNet;

namespace OpenSSHALib.Models;

public class SshPublicKey(string absoluteFilePath) : SshKey(absoluteFilePath), ISshPublicKey
{
    public ISshKey PrivateKey { get; protected set; } = new SshPrivateKey(absoluteFilePath.Replace(".pub", ""));
    
    public void DeleteKey()
    {
        File.Delete(AbsoluteFilePath);
        File.Delete(PrivateKey.AbsoluteFilePath);
    }

    public override IPrivateKeySource GetRenciKeyType() => PrivateKey.GetRenciKeyType();
}