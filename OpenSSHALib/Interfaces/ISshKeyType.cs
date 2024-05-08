#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 08.05.2024 - 22:05:30
// Last edit: 08.05.2024 - 22:05:54

#endregion

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