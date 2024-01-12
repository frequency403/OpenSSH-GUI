namespace OpenSSHALib.Lib;

[Serializable]
public class SettingsFile
{
    public string Version { get; set; }
    public int MaxSavedServers { get; set; }
    public Dictionary<string, string> LastUsedServers { get; set; }
    public string[] FileNamesToSkipWhenSearchingForKeys { get; set; }
}