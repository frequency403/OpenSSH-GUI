#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:32

#endregion

using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Interfaces.AuthorizedKeys;

namespace OpenSSH_GUI.Core.Lib.AuthorizedKeys;

/// <summary>
/// Represents an authorized key entry in an authorized keys file.
/// </summary>
public class AuthorizedKey : IAuthorizedKey
{
    /// <summary>
    /// Represents an authorized key entry in the authorized_keys file.
    /// </summary>
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

    /// <summary>
    /// Represents the key type declaration in the authorized key file.
    /// </summary>
    /// <remarks>
    /// This property holds the key type declaration as it appears in the authorized key file.
    /// The key type declaration indicates the algorithm used for the key.
    /// </remarks>
    private string KeyTypeDeclarationInFile { get; }

    /// <summary>
    /// Represents the type of an authorized key.
    /// </summary>
    public KeyType KeyType { get; }

    /// <summary>
    /// Represents an authorized key entry.
    /// </summary>
    public string Fingerprint { get; }

    /// <summary>
    /// Represents an authorized key entry.
    /// </summary>
    public string Comment { get; }

    /// <summary>
    /// Represents an authorized key.
    /// </summary>
    public bool MarkedForDeletion { get; set; }

    /// <summary>
    /// Gets the full key entry string of an authorized key.
    /// </summary>
    /// <remarks>
    /// The full key entry string consists of the key type, fingerprint, and comment separated by spaces.
    /// </remarks>
    /// <returns>The full key entry string.</returns>
    public string GetFullKeyEntry => $"{KeyTypeDeclarationInFile} {Fingerprint} {Comment}";
}