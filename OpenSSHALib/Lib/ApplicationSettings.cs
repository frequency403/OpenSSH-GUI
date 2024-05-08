using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using OpenSSHALib.Interfaces;

namespace OpenSSHALib.Lib;

public class ApplicationSettings : IApplicationSettings
{
    private ILogger _logger;
    
    public ApplicationSettings(ILogger<IApplicationSettings> logger)
    {
        _logger = logger;
        try
        {
            if (!Directory.Exists(SettingsFileBasePath)) Directory.CreateDirectory(SettingsFileBasePath);
            if (!File.Exists(SettingsFilePath)) WriteCurrentSettingsToFile(FileMode.CreateNew);
            using var streamReader = new StreamReader(SettingsFilePath);
            Settings = JsonSerializer.Deserialize<SettingsFile>(streamReader.ReadToEnd()) ?? new SettingsFile();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error while initializing application settings! Continuing with default settings");
        }
    }
    
    private static string SettingsFileName => AppDomain.CurrentDomain.FriendlyName + ".json";
    private string SettingsFileBasePath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppDomain.CurrentDomain.FriendlyName);
    private string SettingsFilePath => Path.Combine(SettingsFileBasePath, SettingsFileName);
    private bool FileOverflowCheck => Settings.LastUsedServers.Count > Settings.MaxSavedServers;

    public ISettingsFile Settings { get; private set; } = new SettingsFile();
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true
    };

    private readonly SemaphoreSlim _semaphoreSlim = new (1, 1);
    private void WriteCurrentSettingsToFile(FileMode mode = FileMode.Truncate) => WriteCurrentSettingsToFileAsync(mode).Wait();
    private async Task WriteCurrentSettingsToFileAsync(FileMode mode = FileMode.Truncate)
    {
        try
        {
            await _semaphoreSlim.WaitAsync();
            await using var streamWriter = new StreamWriter(SettingsFilePath, Encoding.UTF8,
                new FileStreamOptions { Mode = mode, Access = FileAccess.ReadWrite, Share = FileShare.ReadWrite});
            await streamWriter.WriteAsync(JsonSerializer.Serialize(Settings, _jsonSerializerOptions));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while writing to settings file!");
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }
    

    private bool ShrinkKnownServers()
    {
        try
        {
            var baseValue = Settings.LastUsedServers.Count - Settings.MaxSavedServers;
            for (var i = 0; i < baseValue; i++) Settings.LastUsedServers.Remove(Settings.LastUsedServers.First().Key);
            WriteCurrentSettingsToFile();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error shrinking known servers");
            return false;
        }

        return true;
    }

    public bool AddKnownServerToFile(string host, string username) => AddKnownServerToFileAsync(host, username).Result;
    public async Task<bool> AddKnownServerToFileAsync(string host, string username)
    {
        try
        {
            if (FileOverflowCheck)
            {
                Settings.LastUsedServers.Remove(Settings.LastUsedServers.First().Key);
            }
            Settings.LastUsedServers.Add(host, username);
            await WriteCurrentSettingsToFileAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error adding known server to file");
            return false;
        }

        return true;
    }
}