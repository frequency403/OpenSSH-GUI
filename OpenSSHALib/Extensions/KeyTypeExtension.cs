using OpenSSHALib.Enums;
using OpenSSHALib.Models;

namespace OpenSSHALib.Extensions;

public static class KeyTypeExtension
{
    public static IEnumerable<SshKeyType> GetAvailableKeyTypes()
    {
        return Enum.GetValues<KeyType>().Select(keyType => new SshKeyType(keyType)).ToList();
    }


    public static IEnumerable<int> GetBitValues(this KeyType type)
    {
        return type switch
        {
            KeyType.RSA => [1024, 2048, 3072, 4096],
            KeyType.DSA => [1024],
            KeyType.ECDSA => [256, 384, 521],
            _ => []
        };
    }
}