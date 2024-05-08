#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 08.05.2024 - 22:05:30
// Last edit: 08.05.2024 - 22:05:53

#endregion

namespace OpenSSHALib.Interfaces;

public interface IApplicationSettings
{
    ISettingsFile Settings { get; }
    bool AddKnownServerToFile(string host, string username);
    Task<bool> AddKnownServerToFileAsync(string host, string username);
}