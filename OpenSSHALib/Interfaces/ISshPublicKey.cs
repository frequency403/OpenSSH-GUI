using OpenSSHALib.Models;

namespace OpenSSHALib.Interfaces;

public interface ISshPublicKey : ISshKey
{
    ISshKey PrivateKey { get; }
}