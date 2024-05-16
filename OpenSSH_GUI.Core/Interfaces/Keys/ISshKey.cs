#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:33

#endregion

using OpenSSH_GUI.Core.Interfaces.Misc;

namespace OpenSSH_GUI.Core.Interfaces.Keys;

public interface ISshKey : IKeyBase
{
    string KeyTypeString { get; }
    string Comment { get; }
    bool IsPublicKey { get; }
    bool IsPuttyKey { get; }
    ISshKeyType KeyType { get; }
}