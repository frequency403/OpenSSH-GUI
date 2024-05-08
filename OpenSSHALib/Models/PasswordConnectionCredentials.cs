using OpenSSHALib.Interfaces;
using Renci.SshNet;

namespace OpenSSHALib.Models;
public class PasswordConnectionCredentials : ConnectionCredentials, IPasswordConnectionCredentials
{
    public string Password { get; init; }
    public override ConnectionInfo GetConnectionInfo()
    {
        return new PasswordConnectionInfo(Hostname, Username, Password);
    }
}