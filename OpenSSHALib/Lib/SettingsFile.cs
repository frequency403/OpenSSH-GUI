#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 08.05.2024 - 22:05:30
// Last edit: 08.05.2024 - 22:05:07

#endregion

using System.Diagnostics;
using System.Reflection;
using OpenSSHALib.Interfaces;

namespace OpenSSHALib.Lib;

[Serializable]
public record SettingsFile : ISettingsFile
{
    public string Version { get; set; } =
        $"v{FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly()!.Location).FileVersion}";

    public bool ConvertPpkAutomatically { get; set; } = false;
    public int MaxSavedServers { get; set; } = 5;
    public Dictionary<string, string> LastUsedServers { get; set; } = [];
}