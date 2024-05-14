#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 08.05.2024 - 22:05:30
// Last edit: 14.05.2024 - 03:05:39

#endregion

using ReactiveUI;

namespace OpenSSH_GUI.Core.Interfaces.KnownHosts;

public interface IKnownHost : IReactiveObject
{
    string Host { get; }
    bool DeleteWholeHost { get; }
    List<IKnownHostKey> Keys { get; set; }
    void KeysDeletionSwitch();
    string GetAllEntries();
}