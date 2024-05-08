#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 08.05.2024 - 22:05:30
// Last edit: 08.05.2024 - 22:05:53

#endregion

using ReactiveUI;

namespace OpenSSHALib.Interfaces;

public interface IKnownHost : IReactiveObject
{
    string Host { get; }
    bool DeleteWholeHost { get; }
    List<IKnownHostKey> Keys { get; set; }
    void KeysDeletionSwitch();
    string GetAllEntries();
}