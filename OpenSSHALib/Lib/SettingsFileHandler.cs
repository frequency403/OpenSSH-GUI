using System.Reflection;
using System.Text;
using System.Text.Json;

namespace OpenSSHALib.Lib;

public static class SettingsFileHandler
{
    private static readonly string SettingsFileName = "OpenSSH-GUI.settings";

    private static readonly string SettingsFileBasePath =
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
        Path.DirectorySeparatorChar + Assembly.GetEntryAssembly().GetName().Name;

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true
    };

    private static string SettingsFilePath => SettingsFileBasePath + Path.DirectorySeparatorChar + SettingsFileName;

    public static SettingsFile Settings { get; private set; }

    public static bool InitSettingsFile()
    {
        try
        {
            if (!Directory.Exists(SettingsFileBasePath)) Directory.CreateDirectory(SettingsFileBasePath);

            if (!File.Exists(SettingsFilePath))
            {
                var file = File.Open(SettingsFilePath, FileMode.Create, FileAccess.Write);

                Settings = new SettingsFile
                {
                    UserSshFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) +
                                        $"{Path.DirectorySeparatorChar}.ssh",
                    FileNamesToSkipWhenSearchingForKeys = ["authorized", "config", "known"]
                };
                Settings.KnownHostsFilePath = Settings.UserSshFolderPath + $"{Path.DirectorySeparatorChar}known_hosts";
                file.Write(Encoding.Default.GetBytes(JsonSerializer.Serialize(Settings, JsonSerializerOptions)));
                file.Close();
            }

            using var settingsFile = File.OpenRead(SettingsFilePath);
            using var streamReader = new StreamReader(settingsFile);
            var fileContent = JsonSerializer.Deserialize<SettingsFile>(streamReader.ReadToEnd(), JsonSerializerOptions);
            if (fileContent is null) return false;
            Settings = fileContent;
            return true;
        }
        catch (Exception e)
        {
            return false;
        }
    }
}