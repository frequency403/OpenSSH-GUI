using OpenSSHALib.Interfaces;
using Renci.SshNet;

namespace OpenSSHALib.Models;
public class KeyConnectionCredentials : ConnectionCredentials, IKeyConnectionCredentials
{
    public ISshKey PublicKey { get; init; }
    public override ConnectionInfo GetConnectionInfo()
    {
        return new PrivateKeyConnectionInfo(Hostname, Username, ProxyTypes.None, "", 0, PublicKey.GetRenciKeyType());
    }
}