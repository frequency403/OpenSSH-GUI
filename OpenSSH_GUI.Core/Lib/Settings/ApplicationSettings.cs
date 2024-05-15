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

public class ApplicationSettings : IApplicationSettings
{
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true
    };

    private readonly ILogger _logger;

    public ApplicationSettings(ILogger<IApplicationSettings> logger)
    {
        Crawler = new DirectoryCrawler(logger, Settings);
        _jsonSerializerOptions.Converters.Add(new ConnectionCredentialsConverter(Crawler));
        _logger = logger;
        try
        {
            if (!Directory.Exists(SettingsFileBasePath)) Directory.CreateDirectory(SettingsFileBasePath);
            if (!File.Exists(SettingsFilePath)) WriteCurrentSettingsToFile();
            var deserialized =
                JsonSerializer.Deserialize<SettingsFile>(File.ReadAllText(SettingsFilePath), _jsonSerializerOptions);
            if (deserialized is null || !string.Equals(deserialized.Version, CurrentVersion))
                WriteCurrentSettingsToFile();
            else
                Settings = deserialized;
            DecryptAllPasswords();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error while initializing application settings! Continuing with default settings");
        }

        Crawler.SettingsFile = Settings;
    }

    private string CurrentVersion { get; } =
        $"v{FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly()!.Location).FileVersion}";

    private static string SettingsFileName => AppDomain.CurrentDomain.FriendlyName + ".json";

    private string SettingsFileBasePath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppDomain.CurrentDomain.FriendlyName);

    private string SettingsFilePath => Path.Combine(SettingsFileBasePath, SettingsFileName);
    private bool FileOverflowCheck => Settings.LastUsedServers.Count() > Settings.MaxSavedServers;
    public DirectoryCrawler Crawler { get; }

    public ISettingsFile Settings { get; } = new SettingsFile();

    public bool AddKnownServerToFile(IConnectionCredentials credentials)
    {
        return AddKnownServerToFileAsync(credentials).Result;
    }

    public async Task<bool> AddKnownServerToFileAsync(IConnectionCredentials credentials)
    {
        try
        {
            ShrinkKnownServers();

            var found = Settings.LastUsedServers.FirstOrDefault(e => string.Equals(e.Hostname, credentials.Hostname));
            if (found is not null && string.Equals(found.Username, credentials.Username) &&
                found.AuthType.Equals(credentials.AuthType)) return false;
            Settings.LastUsedServers = Settings.LastUsedServers.Append(credentials);
            await WriteCurrentSettingsToFileAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error adding known server to file");
            return false;
        }

        return true;
    }

    private void EncryptAllPasswords()
    {
        Settings.LastUsedServers = Settings.LastUsedServers.Select(e =>
        {
            if (e is IPasswordConnectionCredentials { EncryptedPassword: false } pwcc) pwcc.EncryptPassword();
            return e;
        }).ToList();
    }

    private void DecryptAllPasswords()
    {
        Settings.LastUsedServers = Settings.LastUsedServers.Select(e =>
        {
            if (e is IPasswordConnectionCredentials { EncryptedPassword: true } pwcc) pwcc.DecryptPassword();
            return e;
        }).ToList();
    }

    private void WriteCurrentSettingsToFile()
    {
        WriteCurrentSettingsToFileAsync().Wait();
    }

    private async Task WriteCurrentSettingsToFileAsync()
    {
        try
        {
            EncryptAllPasswords();
            var serialized = JsonSerializer.Serialize(Settings, _jsonSerializerOptions);
            await File.WriteAllTextAsync(SettingsFilePath, serialized);
            DecryptAllPasswords();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while writing to settings file!");
        }
    }


    private bool ShrinkKnownServers()
    {
        try
        {
            if (!FileOverflowCheck) return false;
            var baseValue = Settings.LastUsedServers.Count() - Settings.MaxSavedServers;
            for (var i = 0; i < baseValue; i++)
                Settings.LastUsedServers = Settings.LastUsedServers.Where(e => e != Settings.LastUsedServers.First());
            WriteCurrentSettingsToFile();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error shrinking known servers");
            return false;
        }

        return true;
    }
}