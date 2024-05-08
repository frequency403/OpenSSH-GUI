#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 08.05.2024 - 22:05:30
// Last edit: 08.05.2024 - 22:05:53

#endregion

using OpenSSHALib.Enums;
using ReactiveUI;

namespace OpenSSHALib.Interfaces;

public interface IKnownHostKey : IReactiveObject
{
    KeyType KeyType { get; }
    string Fingerprint { get; }
    string EntryWithoutHost { get; }
    bool MarkedForDeletion { get; set; }
}