using Renci.SshNet;

namespace OpenSSHALib.Interfaces;

public interface IConnectionCredentials
{
    string Hostname { get; init; }
    string Username { get; init; }
    ConnectionInfo GetConnectionInfo();
}