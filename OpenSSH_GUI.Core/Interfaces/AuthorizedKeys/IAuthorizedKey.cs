#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 08.05.2024 - 22:05:30
// Last edit: 14.05.2024 - 03:05:39

#endregion

using OpenSSH_GUI.Core.Enums;

namespace OpenSSH_GUI.Core.Interfaces.AuthorizedKeys;

public interface IAuthorizedKey
{
    KeyType KeyType { get; }
    string Fingerprint { get; }
    string Comment { get; }
    bool MarkedForDeletion { get; set; }
    string GetFullKeyEntry { get; }
}