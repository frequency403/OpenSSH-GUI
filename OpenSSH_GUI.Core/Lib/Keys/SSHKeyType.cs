#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 08.05.2024 - 22:05:30
// Last edit: 14.05.2024 - 03:05:39

#endregion

using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Interfaces.Keys;

namespace OpenSSH_GUI.Core.Lib.Keys;

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