using System.Diagnostics.CodeAnalysis;
using OpenSSHALib.Models;
using ReactiveUI;

namespace OpenSSHALib.Interfaces;

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