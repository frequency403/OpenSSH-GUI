using OpenSSHALib.Enums;

namespace OpenSSHALib.Model;

public class KnownHosts
{
    public string Host { get; private set; }
    public KeyType KeyType { get; private set; }
    public string TypeDeclarationInFile { get; set; }
    public string Fingerprint { get; private set; }
    public string FullHostEntry { get; private set; }

    public bool MarkedForDeletion { get; set; } = false;
    
    public KnownHosts(string fileEntry)
    {
        FullHostEntry = fileEntry;
        var splitted = FullHostEntry.Split(' ');
        Host = splitted[0];

        TypeDeclarationInFile = splitted[1];
        KeyType = Enum.Parse<KeyType>(TypeDeclarationInFile.StartsWith("ssh-") ? TypeDeclarationInFile.Replace("ssh-", "") : TypeDeclarationInFile.Split('-')[0], true);
        Fingerprint = splitted[2];
    }
}