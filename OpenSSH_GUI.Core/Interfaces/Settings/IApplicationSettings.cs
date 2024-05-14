#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 08.05.2024 - 22:05:30
// Last edit: 14.05.2024 - 03:05:39

#endregion

namespace OpenSSH_GUI.Core.Interfaces.Settings;

public interface IApplicationSettings
{
    ISettingsFile Settings { get; }
    bool AddKnownServerToFile(string host, string username);
    Task<bool> AddKnownServerToFileAsync(string host, string username);
}