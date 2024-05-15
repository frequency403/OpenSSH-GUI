#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:24

#endregion

using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Interfaces.Keys;
using OpenSSH_GUI.Core.Lib.Keys;

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