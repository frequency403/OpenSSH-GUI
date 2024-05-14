#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 08.05.2024 - 22:05:30
// Last edit: 14.05.2024 - 03:05:33

#endregion

using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Interfaces.Keys;
using OpenSSH_GUI.Core.Models;

namespace OpenSSH_GUI.Core.Extensions;

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