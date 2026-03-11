using System.Security.Cryptography;
using SshNet.Keygen;

namespace OpenSSH_GUI.Core.Extensions;

public static class SshKeyTypeExtensions
{
    private static IEnumerable<int> CalculateKeySizes(SshKeyType keyType)
    {
        if (keyType is SshKeyType.ED25519) 
            return [256];
        using AsymmetricAlgorithm algorithm = keyType switch
        {
            SshKeyType.RSA   => RSA.Create(),
            SshKeyType.ECDSA or SshKeyType.ED25519 => ECDsa.Create(),
            _ => throw new ArgumentException("Unsupported key type", nameof(keyType))
        };

        return algorithm.LegalKeySizes
            .SelectMany(ExpandKeyRange);
    }

    private static IEnumerable<int> ExpandKeyRange(KeySizes range)
    {
        if (range.SkipSize == 0)
            return [range.MinSize];

        if(range.MinSize + range.MinSize < range.MaxSize && range.MaxSize % range.MinSize == 0)
            return Enumerable.Range(1, range.MaxSize / range.MinSize)
                .Select(i => i * range.MinSize);
            
        
        return Enumerable
            .Range(0, (range.MaxSize - range.MinSize) / range.SkipSize + 1)
            .Select(i => range.MinSize + i * range.SkipSize);
    }
    
    extension(SshKeyType keyType)
    {
        public IEnumerable<int> SupportedKeySizes => CalculateKeySizes(keyType);
    }
}