#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 08.05.2024 - 22:05:30
// Last edit: 08.05.2024 - 22:05:52

#endregion

using OpenSSHALib.Enums;

namespace OpenSSHALib.Interfaces;

public interface IAuthorizedKey
{
    KeyType KeyType { get; }
    string Fingerprint { get; }
    string Comment { get; }
    bool MarkedForDeletion { get; set; }
    string GetFullKeyEntry { get; }
}