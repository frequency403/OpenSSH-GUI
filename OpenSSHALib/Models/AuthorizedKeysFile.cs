using System.Collections.ObjectModel;
using System.Text;
using DynamicData;
using ReactiveUI;

namespace OpenSSHALib.Models;

public class AuthorizedKeysFile : ReactiveObject
{
    private ObservableCollection<AuthorizedKey> _authorizedKeys = [];
    public ObservableCollection<AuthorizedKey> AuthorizedKeys
    {
        get => _authorizedKeys;
        set => this.RaiseAndSetIfChanged(ref _authorizedKeys, value);
    }
    
    private bool IsFileFromServer { get; }
    private readonly string _fileContentsOrPath;
    
    public AuthorizedKeysFile(string fileContentsOrPath, bool fromServer = false)
    {
        IsFileFromServer = fromServer;
        _fileContentsOrPath = fileContentsOrPath;
        if (IsFileFromServer)
        {
            LoadFileContents(_fileContentsOrPath);
        }
        else
        {
            ReadAndLoadFileContents(_fileContentsOrPath);
        }
    }

    private void LoadFileContents(string fileContents)
    {
        AuthorizedKeys =
            new ObservableCollection<AuthorizedKey>(fileContents.TrimEnd().Split("\r\n", StringSplitOptions.RemoveEmptyEntries)
                .Where(e => e != "").Select(e => new AuthorizedKey(e.Trim())));
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

    public bool ApplyChanges(IEnumerable<AuthorizedKey> keys)
    {
        var countBefore = AuthorizedKeys.Count;
        AuthorizedKeys = new ObservableCollection<AuthorizedKey>(keys.Where(e => !e.MarkedForDeletion));
        return countBefore != AuthorizedKeys.Count;
    }
    
    public AuthorizedKeysFile PersistChangesInFile()
    {
        if (IsFileFromServer) return this;
        using var fileStream = File.Open(_fileContentsOrPath, FileMode.Truncate);
        fileStream.Write(Encoding.Default.GetBytes(ExportFileContent()));
        fileStream.Close();
        ReadAndLoadFileContents(_fileContentsOrPath);
        return this;
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
            AuthorizedKeys.Where(e => !e.MarkedForDeletion).Aggregate("", (s, key) => s += $"{key.GetFullKeyEntry}\r\n") : 
            AuthorizedKeys.Where(e => !e.MarkedForDeletion).Aggregate("", (s, key) => s += $"{key.GetFullKeyEntry}{((platform ??= Environment.OSVersion.Platform) != PlatformID.Unix ? "`r`n" : "\r\n")}");
}