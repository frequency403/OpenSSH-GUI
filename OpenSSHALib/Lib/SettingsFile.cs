using System.Diagnostics;
using System.Reflection;

namespace OpenSSHALib.Lib;

[Serializable]
public class SettingsFile
{
    public string Version { get; set; } = $"v{FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly()!.Location).FileVersion}";
    public int MaxSavedServers { get; set; } = 5;
    public Dictionary<string, string> LastUsedServers { get; set; } = [];
}