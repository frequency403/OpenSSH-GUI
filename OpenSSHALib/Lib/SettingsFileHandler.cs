using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace OpenSSHALib.Lib;

public class SettingsFileHandler
{
    private static readonly Lazy<SettingsFileHandler> Lazy = new(() => new SettingsFileHandler());
    public static SettingsFileHandler Instance => Lazy.Value;
    
    private static string SettingsFileName => AppDomain.CurrentDomain.FriendlyName + ".json";
    private readonly string _assemblyLocation;
    private string SettingsFileBasePath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            Path.GetFileNameWithoutExtension(_assemblyLocation));
    private string SettingsFilePath => Path.Combine(SettingsFileBasePath, SettingsFileName);

    
    public bool IsFileInitialized { get; private set; }
    private bool FileOverflowCheck => Settings.LastUsedServers.Count > Settings.MaxSavedServers;

    public SettingsFile Settings { get; private set; } = new();
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true
    };

    private readonly SemaphoreSlim _semaphoreSlim = new (1, 1);
    public SettingsFileHandler(bool deleteBeforeInit = false)
    {
        _assemblyLocation = Assembly.GetEntryAssembly()!.Location;
        try
        {
            if (!Directory.Exists(SettingsFileBasePath)) Directory.CreateDirectory(SettingsFileBasePath);
            if (File.Exists(SettingsFilePath) && deleteBeforeInit) File.Delete(SettingsFilePath);
            if (!File.Exists(SettingsFilePath)) WriteCurrentSettingsToFile(FileMode.CreateNew);
            using var streamReader = new StreamReader(SettingsFilePath);
            Settings = JsonSerializer.Deserialize<SettingsFile>(streamReader.ReadToEnd()) ?? new SettingsFile();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

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
            Console.WriteLine(e);
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
            Debug.WriteLine(e);
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
            Debug.WriteLine(e.Message);
            return false;
        }

        return true;
    }
}