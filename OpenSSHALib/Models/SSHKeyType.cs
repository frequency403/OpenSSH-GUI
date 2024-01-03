using OpenSSHALib.Enums;
using OpenSSHALib.Extensions;

namespace OpenSSHALib.Models;

public class SshKeyType
{
    public SshKeyType(KeyType baseType, int? currentBitSize = null)
    {
        BaseType = baseType;
        KeyTypeText = Enum.GetName(BaseType);
        var possibleBitSizes = BaseType.GetBitValues().ToList();
        PossibleBitSizes = possibleBitSizes;
        MaxBitSize = possibleBitSizes.Max();
        MinBitSize = possibleBitSizes.Min();
        DefaultBitSize = (int)BaseType;
        CurrentBitSize = currentBitSize ?? DefaultBitSize;
    }

    public KeyType BaseType { get; }
    public string KeyTypeText { get; private set; }
    public int MaxBitSize { get; private set; }
    public int MinBitSize { get; private set; }
    public int DefaultBitSize { get; }
    public int CurrentBitSize { get; set; }
    public IEnumerable<int> PossibleBitSizes { get; private set; }
}