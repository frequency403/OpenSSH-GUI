using OpenSSHALib.Enums;
using OpenSSHALib.Extensions;

namespace OpenSSHALib.Model;

public class SshKeyType
{
    public KeyType BaseType { get; private set; }
    public string KeyTypeText { get; private set; }
    public int MaxBitSize { get; private set; }
    public int MinBitSize { get; private set; }
    public int DefaultBitSize { get; private set; }
    public int CurrentBitSize { get; set; }
    public IEnumerable<int> PossibleBitSizes { get; private set; }
    
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
}