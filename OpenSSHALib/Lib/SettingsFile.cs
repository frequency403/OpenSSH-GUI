namespace OpenSSHALib.Lib;

[Serializable]
public class SettingsFile
{
    public string Version { get; set; } = null!;
    public int MaxSavedServers { get; set; }
    public Dictionary<string, string> LastUsedServers { get; set; } = null!;
}