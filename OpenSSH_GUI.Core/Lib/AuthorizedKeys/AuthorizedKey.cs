#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:32

#endregion

using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Interfaces.AuthorizedKeys;

namespace OpenSSH_GUI.Core.Lib.AuthorizedKeys;

public class AuthorizedKey : IAuthorizedKey
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

    private string KeyTypeDeclarationInFile { get; }

    public KeyType KeyType { get; }
    public string Fingerprint { get; }
    public string Comment { get; }
    public bool MarkedForDeletion { get; set; }
    public string GetFullKeyEntry => $"{KeyTypeDeclarationInFile} {Fingerprint} {Comment}";
}