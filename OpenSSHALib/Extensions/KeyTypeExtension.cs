#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 08.05.2024 - 22:05:30
// Last edit: 08.05.2024 - 22:05:02

#endregion

using OpenSSHALib.Enums;
using OpenSSHALib.Interfaces;
using OpenSSHALib.Models;

namespace OpenSSHALib.Extensions;

public static class KeyTypeExtension
{
    public static IEnumerable<ISshKeyType> GetAvailableKeyTypes()
    {
        return Enum.GetValues<KeyType>().Select(keyType => new SshKeyType(keyType)).ToList();
    }


    public static IEnumerable<int> GetBitValues(this KeyType type)
    {
        return type switch
        {
            KeyType.RSA => [1024, 2048, 3072, 4096],
            KeyType.ECDSA => [256, 384, 521],
            _ => []
        };
    }
}