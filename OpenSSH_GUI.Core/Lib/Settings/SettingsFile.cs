#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:29

#endregion

using System.Diagnostics;
using System.Reflection;
using OpenSSH_GUI.Core.Interfaces.Credentials;
using OpenSSH_GUI.Core.Interfaces.Settings;

namespace OpenSSH_GUI.Core.Lib.Settings;

[Serializable]
public record SettingsFile : ISettingsFile
{
    public string Version { get; set; } =
        $"v{FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly()!.Location).FileVersion}";

    public bool ConvertPpkAutomatically { get; set; } = false;
    public int MaxSavedServers { get; set; } = 5;
    public IEnumerable<IConnectionCredentials> LastUsedServers { get; set; } = [];
}