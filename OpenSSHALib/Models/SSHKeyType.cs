#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 08.05.2024 - 22:05:30
// Last edit: 08.05.2024 - 22:05:55

#endregion

using OpenSSHALib.Enums;
using OpenSSHALib.Extensions;
using OpenSSHALib.Interfaces;

namespace OpenSSHALib.Models;

public class SshKeyType : ISshKeyType
{
    public SshKeyType(KeyType baseType)
    {
        BaseType = baseType;
        KeyTypeText = Enum.GetName(BaseType)!;
        var possibleBitSizes = BaseType.GetBitValues().ToList();
        PossibleBitSizes = possibleBitSizes;
        HasDefaultBitSize = !PossibleBitSizes.Any();
        CurrentBitSize = HasDefaultBitSize ? 0 : PossibleBitSizes.Max();
    }

    public KeyType BaseType { get; }
    public string KeyTypeText { get; }
    public bool HasDefaultBitSize { get; }
    public int CurrentBitSize { get; set; }
    public IEnumerable<int> PossibleBitSizes { get; }
}