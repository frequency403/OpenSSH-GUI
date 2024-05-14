#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 08.05.2024 - 22:05:30
// Last edit: 14.05.2024 - 03:05:38

#endregion

using OpenSSH_GUI.Core.Enums;

namespace OpenSSH_GUI.Core.Interfaces.Keys;

public interface ISshKeyType
{
    KeyType BaseType { get; }
    string KeyTypeText { get; }
    bool HasDefaultBitSize { get; }
    int CurrentBitSize { get; set; }
    IEnumerable<int> PossibleBitSizes { get; }
}