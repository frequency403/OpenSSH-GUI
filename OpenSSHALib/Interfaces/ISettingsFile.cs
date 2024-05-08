namespace OpenSSHALib.Interfaces;

public interface ISettingsFile
{
    string Version { get; set; }
    bool ConvertPpkAutomatically { get; set; }
    int MaxSavedServers { get; set; }
    Dictionary<string, string> LastUsedServers { get; set; }
}