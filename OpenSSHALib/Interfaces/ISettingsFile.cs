#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 08.05.2024 - 22:05:30
// Last edit: 08.05.2024 - 22:05:53

#endregion

namespace OpenSSHALib.Interfaces;

public interface ISettingsFile
{
    string Version { get; set; }
    bool ConvertPpkAutomatically { get; set; }
    int MaxSavedServers { get; set; }
    Dictionary<string, string> LastUsedServers { get; set; }
}