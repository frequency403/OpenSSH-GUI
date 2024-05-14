﻿#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 08.05.2024 - 22:05:30
// Last edit: 14.05.2024 - 03:05:39

#endregion

using OpenSSH_GUI.Core.Enums;
using ReactiveUI;

namespace OpenSSH_GUI.Core.Interfaces.Misc;

public interface IKnownHostKey : IReactiveObject
{
    KeyType KeyType { get; }
    string Fingerprint { get; }
    string EntryWithoutHost { get; }
    bool MarkedForDeletion { get; set; }
}