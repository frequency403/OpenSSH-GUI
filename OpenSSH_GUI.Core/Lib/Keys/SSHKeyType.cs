#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:27

#endregion

using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Interfaces.Keys;

namespace OpenSSH_GUI.Core.Lib.Keys;

/// <summary>
/// Represents an SSH key type.
/// </summary>
public class SshKeyType : ISshKeyType
{
    /// <summary>
    /// Represents a SSH key type.
    /// </summary>
    public SshKeyType(KeyType baseType)
    {
        BaseType = baseType;
        KeyTypeText = Enum.GetName(BaseType)!;
        var possibleBitSizes = BaseType.GetBitValues().ToList();
        PossibleBitSizes = possibleBitSizes;
        HasDefaultBitSize = !PossibleBitSizes.Any();
        CurrentBitSize = HasDefaultBitSize ? 0 : PossibleBitSizes.Max();
    }

    /// <summary>
    /// A class representing the base type of a SSH key.
    /// </summary>
    public KeyType BaseType { get; }
    public string KeyTypeText { get; }

    /// <summary>
    /// Gets a value indicating whether the SSH key type has a default bit size.
    /// </summary>
    /// <remarks>
    /// The SSH key type may have a default bit size if there are no possible bit sizes defined for the key type.
    /// </remarks>
    public bool HasDefaultBitSize { get; }

    /// <summary>
    /// Gets or sets the current bit size of the SSH key type.
    /// </summary>
    public int CurrentBitSize { get; set; }

    /// <summary>
    /// Represents a type of SSH key.
    /// </summary>
    public IEnumerable<int> PossibleBitSizes { get; }
}