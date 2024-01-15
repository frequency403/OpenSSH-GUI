namespace OpenSSHALib.Models;

public class SshPublicKey : SshKey
{
    public SshPublicKey(string absoluteFilePath) : base(absoluteFilePath)
    {
        PrivateKey = new SshPrivateKey(AbsoluteFilePath.Replace(".pub", ""));
    }

    public SshPrivateKey PrivateKey { get; protected set; }

    public void DeleteKey()
    {
        File.Delete(AbsoluteFilePath);
        File.Delete(PrivateKey.AbsoluteFilePath);
    }
}