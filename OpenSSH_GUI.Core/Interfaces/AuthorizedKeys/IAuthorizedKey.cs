#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:35

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