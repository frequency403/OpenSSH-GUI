using OpenSSHALib.Enums;

namespace OpenSSHALib.Extensions;

public static class KeyTypeExtension
{
    public static IEnumerable<int> GetBitValues(this KeyType type)
    {
        var listOfInts = new List<int>();
        for (var i = type.MinimalValue(); i <= (int)type; i *= 2) listOfInts.Add(i);
        if(!listOfInts.Contains((int)type)) listOfInts.Add((int)type);
        return listOfInts;
    }

    private static int MinimalValue(this KeyType type)
    {
        return type switch
        {
            KeyType.RSA or KeyType.DSA => 1024, 
            KeyType.ECDSA => 192,
            KeyType.EdDSA or KeyType.Ed25519 => 128,
            _ => 0
        };
    }
}