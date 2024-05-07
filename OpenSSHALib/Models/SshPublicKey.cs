namespace OpenSSHALib.Models;

public class SshPublicKey(string absoluteFilePath) : SshKey(absoluteFilePath)
{
    public SshPrivateKey PrivateKey { get; protected set; } = new(absoluteFilePath.Replace(".pub", ""));

    public void DeleteKey()
    {
        File.Delete(AbsoluteFilePath);
        File.Delete(PrivateKey.AbsoluteFilePath);
    }
}