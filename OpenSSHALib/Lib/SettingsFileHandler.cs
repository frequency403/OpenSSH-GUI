using System.Diagnostics;
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

    public static bool IsFileInitialized { get; private set; } = false;

    private static void WriteIntoFile()
    {
        using var file = File.Open(SettingsFilePath, FileMode.Create, FileAccess.Write);

        Settings = new SettingsFile
        {
            Version = $"v{FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location).FileVersion}",
            MaxSavedServers = 5,
            LastUsedServers = [],
            FileNamesToSkipWhenSearchingForKeys = ["authorized", "config", "known"]
        };
        file.Write(Encoding.Default.GetBytes(JsonSerializer.Serialize(Settings, JsonSerializerOptions)));
    }

    private static bool FileOverflowCheck => Settings.LastUsedServers.Count > Settings.MaxSavedServers;
    
    public static bool InitSettingsFile(bool deleteBeforeInit = false)
    {
        try
        {
            if (!Directory.Exists(SettingsFileBasePath)) Directory.CreateDirectory(SettingsFileBasePath);
            if (File.Exists(SettingsFilePath) && deleteBeforeInit) File.Delete(SettingsFilePath);
            if (!File.Exists(SettingsFilePath)) WriteIntoFile();
            using var settingsFile = File.OpenRead(SettingsFilePath);
            using var streamReader = new StreamReader(settingsFile);
            var fileContent = JsonSerializer.Deserialize<SettingsFile>(streamReader.ReadToEnd(), JsonSerializerOptions);

            if (fileContent is null) return false;
            if (fileContent.Version !=
                $"v{FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location).FileVersion}")
            {
                streamReader.Close();
                settingsFile.Close();
                InitSettingsFile(true);
            }

            Settings = fileContent;
            if (FileOverflowCheck)
            {
                streamReader.Close();
                settingsFile.Close();
                ShrinkKnownServers();
                InitSettingsFile();
            }
            IsFileInitialized = true;
            return IsFileInitialized;
        }
        catch (Exception e)
        {
            return IsFileInitialized;
        }
    }

    private static bool ShrinkKnownServers()
    {
        try
        {
            var baseValue = Settings.LastUsedServers.Count - Settings.MaxSavedServers;
            for (var i = 0; i < baseValue; i++)
            {
                Settings.LastUsedServers.Remove(Settings.LastUsedServers.First().Key);
            }

            using var settingsFile = File.Open(SettingsFilePath, FileMode.Truncate);
            settingsFile.Write(Encoding.Default.GetBytes(JsonSerializer.Serialize(Settings, JsonSerializerOptions)));
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            return false;
        }

        return true;
    }
    
    public static bool AddKnownServerToFile(string host, string username)
    {
        try
        {
            Settings.LastUsedServers.Add(host, username);
            using var settingsFile = File.Open(SettingsFilePath, FileMode.Truncate);
            settingsFile.Write(Encoding.Default.GetBytes(JsonSerializer.Serialize(Settings, JsonSerializerOptions)));
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
            return false;
        }
        return true;
    }

    public static async Task<bool> AddKnownServerToFileAsync(string host, string username)
    {
        try
        {
            Settings.LastUsedServers.Add(host, username);
            await using var settingsFile = File.Open(SettingsFilePath, FileMode.Truncate);
            await settingsFile.WriteAsync(Encoding.Default.GetBytes(JsonSerializer.Serialize(Settings, JsonSerializerOptions)));
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
            return false;
        }
        return true;
    }
}