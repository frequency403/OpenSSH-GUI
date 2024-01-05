using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia;

namespace OpenSSHALib.Lib;

public static class SettingsFileHandler
{
    private static string SettingsFileName = "OpenSSH-GUI.settings";
    private static string SettingsFileBasePath => SettingsFilePath.Replace(Path.DirectorySeparatorChar + SettingsFileName, "");
    private static string SettingsFilePath =>  Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
    Path.DirectorySeparatorChar + Application.Current.Name + Path.DirectorySeparatorChar +SettingsFileName;
    
    public static SettingsFile Settings { get; private set; }

    private static JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true
    };

    public static void InitSettingsFile()
    {
        if (!Directory.Exists(SettingsFileBasePath)) Directory.CreateDirectory(SettingsFileBasePath);
        if (!File.Exists(SettingsFilePath))
        {
            var file = File.Create(SettingsFilePath);

            Settings = new SettingsFile
            {
                UserSshFolderPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) +
                                    $"{Path.DirectorySeparatorChar}.ssh"
            };
            Settings.KnownHostsFilePath = Settings.UserSshFolderPath + $"{Path.DirectorySeparatorChar}known_hosts";
            file.Write(Encoding.Default.GetBytes(JsonSerializer.Serialize(Settings, JsonSerializerOptions)));
        }
        
        
    }
}