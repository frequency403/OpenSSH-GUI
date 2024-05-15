#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:27

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