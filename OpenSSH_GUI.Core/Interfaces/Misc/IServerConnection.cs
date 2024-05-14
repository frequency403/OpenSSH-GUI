﻿#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 08.05.2024 - 22:05:30
// Last edit: 14.05.2024 - 03:05:26

#endregion

using System.Diagnostics.CodeAnalysis;
using ReactiveUI;

namespace OpenSSH_GUI.Core.Interfaces.Misc;

public interface IServerConnection : IReactiveObject, IDisposable
{
    DateTime ConnectionTime { get; set; }
    bool IsConnected { get; set; }
    string ConnectionString { get; }
    PlatformID ServerOs { get; set; }
    bool TestAndOpenConnection([NotNullWhen(false)] out Exception? exception);
    bool CloseConnection([NotNullWhen(false)] out Exception? ex);
    IKnownHostsFile GetKnownHostsFromServer();
    bool WriteKnownHostsToServer(IKnownHostsFile knownHostsFile);
    IAuthorizedKeysFile GetAuthorizedKeysFromServer();
    bool WriteAuthorizedKeysChangesToServer(IAuthorizedKeysFile authorizedKeysFile);
}