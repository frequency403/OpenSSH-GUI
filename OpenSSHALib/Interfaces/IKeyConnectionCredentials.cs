namespace OpenSSHALib.Interfaces;

public interface IKeyConnectionCredentials : IConnectionCredentials
{
    ISshKey PublicKey { get; init; }
}