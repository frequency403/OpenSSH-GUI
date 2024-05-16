#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:27

#endregion

using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using DynamicData.Kernel;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Converter.Json;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Interfaces.Credentials;
using OpenSSH_GUI.Core.Interfaces.Settings;
using OpenSSH_GUI.Core.Lib.Misc;

namespace OpenSSH_GUI.Core.Lib.Settings;

/// <summary>
/// Represents the application settings.
/// </summary>
public class ApplicationSettings(
    ILogger<IApplicationSettings> logger,
    ISettingsFile settingsFile,
    DirectoryCrawler crawler,
    ConnectionCredentialsConverter converter) : IApplicationSettings, IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Provides JSON serialization options for the <see cref="JsonSerializer"/> instance.
    /// </summary>
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true,
        Converters = { converter }
    };

    /// <summary>
    /// Initializes the application settings.
    /// </summary>
    public void Init()
    {
        try
        {
            CreateSettingsDirectory();
            var settings = GetDeserializedSettings();
            UpdateServerCredentials(settings);
            AttachEventHandlers();
            DecryptAllPasswords();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error while initializing application settings! Continuing with default settings");
        }

        crawler.Refresh();
    }

    /// <summary>
    /// Creates the settings directory if it does not exist.
    /// </summary>
    private void CreateSettingsDirectory()
    {
        if (!Directory.Exists(SettingsFileBasePath))
            Directory.CreateDirectory(SettingsFileBasePath);
    }

    /// <summary>
    /// Retrieves the deserialized settings from the settings file.
    /// </summary>
    /// <returns>The deserialized settings.</returns>
    private SettingsFile GetDeserializedSettings()
    {
        if (!File.Exists(SettingsFilePath))
            WriteCurrentSettingsToFile();

        return JsonSerializer.Deserialize<SettingsFile>(File.ReadAllText(SettingsFilePath), _jsonSerializerOptions);
    }

    /// <summary>
    /// Updates the server credentials in the settings file.
    /// </summary>
    /// <param name="settings">The settings file.</param>
    private void UpdateServerCredentials(SettingsFile settings)
    {
        if (settings is null || !string.Equals(settings.Version, CurrentVersion))
        {
            WriteCurrentSettingsToFile();
            return;
        }

        var duplicates = settings.LastUsedServers
            .Where(e => e.AuthType == AuthType.Key)
            .Select(f => f as IKeyConnectionCredentials)
            .Duplicates(g => g.Hostname)
            .Duplicates(h => h.Username)
            .ToArray();

        if (duplicates.Length > 0)
        {
            foreach (var item in duplicates)
                settings.LastUsedServers.Remove(item);

            var converted = duplicates.ToMultiKeyConnectionCredentials();
            settings.LastUsedServers.Add(converted);
            crawler.UpdateKeys(converted);
        }

        settingsFile.ChangeSettings(settings);
    }

    /// <summary>
    /// Attaches event handlers to the SettingsChanged event of the settings file.
    /// </summary>
    private void AttachEventHandlers()
    {
        settingsFile.SettingsChanged += (sender, args) =>
        {
            WriteCurrentSettingsToFile();
            return args;
        };
    }

    /// <summary>
    /// Gets the current version of the application.
    /// </summary>
    /// <remarks>
    /// The version is determined from the entry assembly or the executing assembly.
    /// </remarks>
    private string CurrentVersion { get; } =
        $"v{(Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly()).GetName().Version.ToString(3)}";

    /// <summary>
    /// Gets the filename of the settings file.
    /// </summary>
    private static string SettingsFileName => AppDomain.CurrentDomain.FriendlyName + ".json";

    /// <summary>
    /// Represents the base path for the settings file.
    /// </summary>
    private string SettingsFileBasePath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppDomain.CurrentDomain.FriendlyName);

    /// <summary>
    /// Gets the file path of the settings file.
    /// </summary>
    private string SettingsFilePath => Path.Combine(SettingsFileBasePath, SettingsFileName);

    /// <summary>
    /// Specifies whether the file containing the last used servers in the application has overflowed its capacity.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="FileOverflowCheck"/> property checks whether the number of last used servers in the settings file
    /// exceeds the maximum number of saved servers. If it does, the property returns <c>true</c>; otherwise, it returns <c>false</c>.
    /// </para>
    /// <para>
    /// This property is used in the <see cref="ShrinkKnownServers"/> method to remove the excess last used servers and write the updated
    /// settings to the file.
    /// </para>
    /// </remarks>
    private bool FileOverflowCheck => settingsFile.LastUsedServers.Count() > settingsFile.MaxSavedServers;

    /// <summary>
    /// Adds a known server to the settings file.
    /// </summary>
    /// <param name="credentials">The connection credentials of the server to add.</param>
    /// <returns>True if the server was added successfully, false otherwise.</returns>
    public bool AddKnownServerToFile(IConnectionCredentials credentials)
    {
        return AddKnownServerToFileAsync(credentials).Result;
    }

    /// <summary>
    /// Adds a known server to the settings file asynchronously.
    /// </summary>
    /// <param name="credentials">The connection credentials for the server.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains a boolean indicating whether the server was added successfully or not.</returns>
    public async Task<bool> AddKnownServerToFileAsync(IConnectionCredentials credentials)
    {
        try
        {
            ShrinkKnownServers();
            var found = settingsFile.LastUsedServers.Where(e =>
                string.Equals(e.Hostname, credentials.Hostname) && string.Equals(e.Username, credentials.Username));
            if (found.Any(e => e.AuthType.Equals(credentials.AuthType))) return false;
            if (credentials is IMultiKeyConnectionCredentials multiKeyConnectionCredentials)
            {
                settingsFile.LastUsedServers.AddRange(multiKeyConnectionCredentials.ToKeyConnectionCredentials());
                crawler.UpdateKeys(multiKeyConnectionCredentials);
            }
            else
            {
                settingsFile.LastUsedServers.Add(credentials);
            }
            await WriteCurrentSettingsToFileAsync();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error adding known server to file");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Encrypts the passwords of all the servers in the settings file.
    /// </summary>
    private void EncryptAllPasswords()
    {
        foreach (var server in settingsFile.LastUsedServers)
        {
            server.EncryptPassword();
        }
    }

    /// <summary>
    /// Decrypts the passwords of all servers in the settings file.
    /// </summary>
    private void DecryptAllPasswords()
    {
        foreach (var server in settingsFile.LastUsedServers)
        {
           server.DecryptPassword();
        }
    }

    /// <summary>
    /// Writes the current application settings to a file.
    /// </summary>
    public void WriteCurrentSettingsToFile()
    {
        WriteCurrentSettingsToFileAsync().Wait();
    }

    /// <summary>
    /// Writes the current settings to a file asynchronously.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. It returns void when completed.</returns>
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

    /// <summary>
    /// Shrinks the list of known servers if it exceeds the maximum allowed number.
    /// </summary>
    /// <returns>True if the list was successfully shrunk, false otherwise.</returns>
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

    /// <summary>
    /// Releases the resources used by the ApplicationSettings object.
    /// </summary>
    public void Dispose()
    {
        WriteCurrentSettingsToFile();
    }

    /// <summary>
    /// Asynchronously disposes the object and writes the current settings to a file.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async ValueTask DisposeAsync()
    {
        await WriteCurrentSettingsToFileAsync();
    }
}