using OpenSSHALib.Enums;

namespace OpenSSHALib.Interfaces;

public interface ISshKeyType
{
    KeyType BaseType { get; }
    string KeyTypeText { get; }
    bool HasDefaultBitSize { get; }
    int CurrentBitSize { get; set; }
    IEnumerable<int> PossibleBitSizes { get; }
}