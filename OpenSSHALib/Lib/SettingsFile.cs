namespace OpenSSHALib.Lib;

[Serializable]
public class SettingsFile
{
    public string UserSshFolderPath { get; set; }
    public string KnownHostsFilePath { get; set; }
    public string[] FileNamesToSkipWhenSearchingForKeys { get; set; }
}