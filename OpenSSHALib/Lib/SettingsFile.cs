using System.Diagnostics;
using System.Reflection;
using OpenSSHALib.Interfaces;

namespace OpenSSHALib.Lib;

[Serializable]
public record SettingsFile : ISettingsFile
{
    public string Version { get; set; } = $"v{FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly()!.Location).FileVersion}";
    public bool ConvertPpkAutomatically { get; set; } = true;
    public int MaxSavedServers { get; set; } = 5;
    public Dictionary<string, string> LastUsedServers { get; set; } = [];
}