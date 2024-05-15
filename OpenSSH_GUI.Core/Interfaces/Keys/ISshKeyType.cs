#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:33

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