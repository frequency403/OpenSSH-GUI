#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 08.05.2024 - 22:05:30
// Last edit: 14.05.2024 - 03:05:21

#endregion

using System.Diagnostics;
using System.Reflection;
using OpenSSH_GUI.Core.Interfaces.Settings;

namespace OpenSSH_GUI.Core.Lib;

[Serializable]
public record SettingsFile : ISettingsFile
{
    public string Version { get; set; } =
        $"v{FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly()!.Location).FileVersion}";

    public bool ConvertPpkAutomatically { get; set; } = false;
    public int MaxSavedServers { get; set; } = 5;
    public Dictionary<string, string> LastUsedServers { get; set; } = [];
}