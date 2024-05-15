#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:34

#endregion

using OpenSSH_GUI.Core.Interfaces.Credentials;

namespace OpenSSH_GUI.Core.Interfaces.Settings;

public interface ISettingsFile
{
    string Version { get; set; }
    bool ConvertPpkAutomatically { get; set; }
    int MaxSavedServers { get; set; }
    IEnumerable<IConnectionCredentials> LastUsedServers { get; set; }
}