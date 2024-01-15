using OpenSSHALib.Enums;

namespace OpenSSHALib.Models;

public class AuthorizedKey
{
    public AuthorizedKey(string keyEntry)
    {
        var split = keyEntry.Split(' ');
        if (split.Length != 3)
            throw new IndexOutOfRangeException("Authorized Keys must contain TYPE FINGERPRINT COMMENT");

        KeyTypeDeclarationInFile = split[0];
        KeyType = Enum.Parse<KeyType>(
            KeyTypeDeclarationInFile.StartsWith("ssh-")
                ? KeyTypeDeclarationInFile.Replace("ssh-", "")
                : KeyTypeDeclarationInFile.Split('-')[0], true);
        Fingerprint = split[1];
        Comment = split[2];
    }

    public KeyType KeyType { get; }
    private string KeyTypeDeclarationInFile { get; }
    public string Fingerprint { get; }
    public string Comment { get; }
    public bool MarkedForDeletion { get; set; }
    public string GetFullKeyEntry => $"{KeyTypeDeclarationInFile} {Fingerprint} {Comment}";
}