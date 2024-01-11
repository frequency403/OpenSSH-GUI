using System.Collections.ObjectModel;
using DynamicData;
using ReactiveUI;

namespace OpenSSHALib.Models;

public class AuthorizedKeysFile : ReactiveObject
{
    public ObservableCollection<AuthorizedKey> AuthorizedKeys { get; set; }= [];
    private bool IsFileFromServer { get; }
    
    public AuthorizedKeysFile(string fileContentsOrPath, bool fromServer = false)
    {
        IsFileFromServer = fromServer;
        if (IsFileFromServer)
        {
            LoadFileContents(fileContentsOrPath);
        }
        else
        {
            ReadAndLoadFileContents(fileContentsOrPath);
        }
    }

    private void LoadFileContents(string fileContents)
    {
        AuthorizedKeys =
            new ObservableCollection<AuthorizedKey>(fileContents.Split("\r\n", StringSplitOptions.RemoveEmptyEntries)
                .Select(e => new AuthorizedKey(e.Trim())));
    }
    
    private void ReadAndLoadFileContents(string pathToFile)
    {
        if (!File.Exists(pathToFile)) File.Create(pathToFile);
        using var fileStream = File.OpenRead(pathToFile);
        using var streamReader = new StreamReader(fileStream);
        LoadFileContents(streamReader.ReadToEnd());
    }

    public bool AddAuthorizedKey(SshPublicKey key)
    {
        if (AuthorizedKeys.Any(e => e.Fingerprint == key.Fingerprint)) return false;
        var export = key.ExportKey();
        if (export is null) return false;
        AuthorizedKeys.Add(new AuthorizedKey(export));
        return true;
    }

    public async Task<bool> AddAuthorizedKeyAsync(SshPublicKey key)
    {
        if (AuthorizedKeys.Any(e => e.Fingerprint == key.Fingerprint)) return false;
        var export = await key.ExportKeyAsync();
        if (export is null) return false;
        AuthorizedKeys.Add(new AuthorizedKey(export));
        return true;
    }

    public bool RemoveAuthorizedKey(SshPublicKey key)
    {
        if (AuthorizedKeys.All(e => e.Fingerprint != key.Fingerprint)) return false;
        {
            AuthorizedKeys.Remove(AuthorizedKeys.First(e => e.Fingerprint == key.Fingerprint));
            return true;
        }
    }

    public string ExportFileContent(bool local = true, PlatformID? platform = null) =>
     local ? 
            AuthorizedKeys.Aggregate("", (s, key) => s += $"{key.GetFullKeyEntry}\r\n") : 
            AuthorizedKeys.Aggregate("", (s, key) => s += $"{key.GetFullKeyEntry}{((platform ??= Environment.OSVersion.Platform) != PlatformID.Unix ? "`r`n" : "\r\n")}");
}