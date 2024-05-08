namespace OpenSSHALib.Interfaces;

public interface IPasswordConnectionCredentials : IConnectionCredentials
{
    string Password { get; init; }
}