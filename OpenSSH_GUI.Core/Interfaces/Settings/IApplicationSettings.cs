#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:33

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