#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 08.05.2024 - 22:05:30
// Last edit: 08.05.2024 - 22:05:55

#endregion

namespace OpenSSHALib.Interfaces;

public interface ISshPublicKey : ISshKey
{
    ISshKey PrivateKey { get; }
}