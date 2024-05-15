#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:34

#endregion

using OpenSSH_GUI.Core.Enums;
using ReactiveUI;

namespace OpenSSH_GUI.Core.Interfaces.KnownHosts;

public interface IKnownHostKey : IReactiveObject
{
    KeyType KeyType { get; }
    string Fingerprint { get; }
    string EntryWithoutHost { get; }
    bool MarkedForDeletion { get; set; }
}