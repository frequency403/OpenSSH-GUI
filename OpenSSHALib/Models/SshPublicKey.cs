namespace OpenSSHALib.Models;

public class SshPublicKey(string absoluteFilePath) : SshKey(absoluteFilePath)
{
    public SshPrivateKey PrivateKey { get; protected set; }

    public void GetPrivateKey()
    {
        if (IsPublicKey) PrivateKey = new SshPrivateKey(AbsoluteFilePath.Replace(".pub", ""));
    }

    public override void DeleteKey()
    {
        File.Delete(AbsoluteFilePath);
        File.Delete(PrivateKey.AbsoluteFilePath);
    }
}