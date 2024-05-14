#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 08.05.2024 - 22:05:30
// Last edit: 14.05.2024 - 03:05:39

#endregion

using OpenSSH_GUI.Core.Interfaces.Credentials;
using OpenSSH_GUI.Core.Lib.Misc;

namespace OpenSSH_GUI.Core.Interfaces.Settings;

public interface IApplicationSettings
{
    DirectoryCrawler Crawler { get; }
    ISettingsFile Settings { get; }
    bool AddKnownServerToFile(IConnectionCredentials credentials);
    Task<bool> AddKnownServerToFileAsync(IConnectionCredentials credentials);
}