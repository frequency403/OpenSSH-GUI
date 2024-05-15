#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:34

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