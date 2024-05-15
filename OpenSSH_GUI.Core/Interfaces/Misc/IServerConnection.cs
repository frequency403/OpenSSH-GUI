#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:33

#endregion

using System.Diagnostics.CodeAnalysis;
using OpenSSH_GUI.Core.Interfaces.AuthorizedKeys;
using OpenSSH_GUI.Core.Interfaces.Credentials;
using OpenSSH_GUI.Core.Interfaces.KnownHosts;
using ReactiveUI;

namespace OpenSSH_GUI.Core.Interfaces.Misc;

public interface IServerConnection : IReactiveObject, IDisposable
{
    IConnectionCredentials ConnectionCredentials { get; }
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