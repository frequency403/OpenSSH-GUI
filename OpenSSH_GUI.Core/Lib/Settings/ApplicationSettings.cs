#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:27

#endregion

using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Converter.Json;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Interfaces.Credentials;
using OpenSSH_GUI.Core.Interfaces.Settings;
using OpenSSH_GUI.Core.Lib.Misc;

namespace OpenSSH_GUI.Core.Lib.Settings;

public class ApplicationSettings(
    ILogger<IApplicationSettings> logger,
    ISettingsFile settingsFile,
    DirectoryCrawler crawler,
    ConnectionCredentialsConverter converter) : IApplicationSettings, IDisposable, IAsyncDisposable
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true,
        Converters = { converter }
    };

    public void Init()
    {
        try
        {
            if (!Directory.Exists(SettingsFileBasePath)) Directory.CreateDirectory(SettingsFileBasePath);
            if (!File.Exists(SettingsFilePath)) WriteCurrentSettingsToFile();
            var deserialized =
                JsonSerializer.Deserialize<SettingsFile>(File.ReadAllText(SettingsFilePath), _jsonSerializerOptions);
            if (deserialized is null || !string.Equals(deserialized.Version, CurrentVersion))
                WriteCurrentSettingsToFile();
            else
                settingsFile.ChangeSettings(deserialized);
            DecryptAllPasswords();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error while initializing application settings! Continuing with default settings");
        }
        settingsFile.SettingsChanged += (sender, args) =>
        {
            WriteCurrentSettingsToFile();
            return args;
        };
        crawler.Refresh();
    }

    private string CurrentVersion { get; } =
        $"v{(Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly()).GetName().Version.ToString(3)}";

    private static string SettingsFileName => AppDomain.CurrentDomain.FriendlyName + ".json";

    private string SettingsFileBasePath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppDomain.CurrentDomain.FriendlyName);

    private string SettingsFilePath => Path.Combine(SettingsFileBasePath, SettingsFileName);
    private bool FileOverflowCheck => settingsFile.LastUsedServers.Count() > settingsFile.MaxSavedServers;
    
    public bool AddKnownServerToFile(IConnectionCredentials credentials)
    {
        return AddKnownServerToFileAsync(credentials).Result;
    }

    public async Task<bool> AddKnownServerToFileAsync(IConnectionCredentials credentials)
    {
        try
        {
            ShrinkKnownServers();
            var found = settingsFile.LastUsedServers.Where(e =>
                string.Equals(e.Hostname, credentials.Hostname) && string.Equals(e.Username, credentials.Username));
            if (found.Any(e => e.AuthType.Equals(credentials.AuthType))) return false;
            
            settingsFile.LastUsedServers.Add(credentials);
            await WriteCurrentSettingsToFileAsync();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error adding known server to file");
            return false;
        }

        return true;
    }

    private void EncryptAllPasswords()
    {
        foreach (var server in settingsFile.LastUsedServers)
        {
            if (server is IPasswordConnectionCredentials { EncryptedPassword: false } pwcc) pwcc.EncryptPassword();
        }
    }

    private void DecryptAllPasswords()
    {
        foreach (var server in settingsFile.LastUsedServers)
        {
            if (server is IPasswordConnectionCredentials { EncryptedPassword: true } pwcc) pwcc.DecryptPassword();
        }
    }

    public void WriteCurrentSettingsToFile()
    {
        WriteCurrentSettingsToFileAsync().Wait();
    }

    public async Task WriteCurrentSettingsToFileAsync()
    {
        try
        {
            EncryptAllPasswords();
            var serialized = JsonSerializer.Serialize(settingsFile, _jsonSerializerOptions);
            await File.WriteAllTextAsync(SettingsFilePath, serialized);
            DecryptAllPasswords();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error while writing to settings file!");
        }
    }

    private bool ShrinkKnownServers()
    {
        try
        {
            if (!FileOverflowCheck) return false;
            var baseValue = settingsFile.LastUsedServers.Count - settingsFile.MaxSavedServers;
            for (var i = 0; i < baseValue; i++)
                settingsFile.LastUsedServers.RemoveAt(i);
            WriteCurrentSettingsToFile();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error shrinking known servers");
            return false;
        }

        return true;
    }

    public void Dispose()
    {
        WriteCurrentSettingsToFile();
    }

    public async ValueTask DisposeAsync()
    {
        await WriteCurrentSettingsToFileAsync();
    }
}