using OpenSSHALib.Interfaces;
using Renci.SshNet;

namespace OpenSSHALib.Models;
[Serializable]
public abstract class ConnectionCredentials : IConnectionCredentials
{
    public string Hostname { get; init; }
    public string Username { get; init; }
    public abstract ConnectionInfo GetConnectionInfo();
}