using OpenSSHALib.Enums;
using OpenSSHALib.Extensions;

namespace OpenSSHALib.Models;

public class SshKeyType
{
    public SshKeyType(KeyType baseType)
    {
        BaseType = baseType;
        KeyTypeText = Enum.GetName(BaseType)!;
        var possibleBitSizes = BaseType.GetBitValues().ToList();
        PossibleBitSizes = possibleBitSizes;
        HasDefaultBitSize = !PossibleBitSizes.Any();
        CurrentBitSize = HasDefaultBitSize ? 0 : PossibleBitSizes.Max();
    }

    public KeyType BaseType { get; }
    public string KeyTypeText { get; private set; }
    public bool HasDefaultBitSize { get; }
    public int CurrentBitSize { get; set; }
    public IEnumerable<int> PossibleBitSizes { get; }
}