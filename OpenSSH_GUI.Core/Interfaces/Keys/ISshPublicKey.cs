#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 08.05.2024 - 22:05:30
// Last edit: 14.05.2024 - 03:05:39

#endregion

namespace OpenSSH_GUI.Core.Interfaces.Keys;

public interface ISshPublicKey : ISshKey
{
    ISshKey PrivateKey { get; }
}