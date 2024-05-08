using OpenSSHALib.Lib;

namespace OpenSSHALib.Interfaces;

public interface IApplicationSettings
{
    ISettingsFile Settings { get; }
    bool AddKnownServerToFile(string host, string username);
    Task<bool> AddKnownServerToFileAsync(string host, string username);
}