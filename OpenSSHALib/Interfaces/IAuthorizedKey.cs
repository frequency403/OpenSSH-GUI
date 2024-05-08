using OpenSSHALib.Enums;

namespace OpenSSHALib.Interfaces;

public interface IAuthorizedKey
{
    KeyType KeyType { get; }
    string Fingerprint { get; }
    string Comment { get; }
    bool MarkedForDeletion { get; set; }
    string GetFullKeyEntry { get; }
}