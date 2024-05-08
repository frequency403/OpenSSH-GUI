using Renci.SshNet;
namespace OpenSSHALib.Models;

public class SshPrivateKey(string absoluteFilePath) : SshKey(absoluteFilePath)
{
    public override IPrivateKeySource GetRenciKeyType()
    {
        return new PrivateKeyFile(AbsoluteFilePath);
    }
}