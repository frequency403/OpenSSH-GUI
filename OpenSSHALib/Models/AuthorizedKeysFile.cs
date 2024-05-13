#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 08.05.2024 - 22:05:30
// Last edit: 08.05.2024 - 22:05:58

#endregion

using System.Collections.ObjectModel;
using System.Text;
using OpenSSHALib.Interfaces;
using ReactiveUI;

namespace OpenSSHALib.Models;

public class AuthorizedKeysFile : ReactiveObject, IAuthorizedKeysFile
{
    private readonly string _fileContentsOrPath;
    private ObservableCollection<IAuthorizedKey> _authorizedKeys = [];

    public AuthorizedKeysFile(string fileContentsOrPath, bool fromServer = false)
    {
        IsFileFromServer = fromServer;
        _fileContentsOrPath = fileContentsOrPath;
        if (IsFileFromServer)
            LoadFileContents(_fileContentsOrPath);
        else
            ReadAndLoadFileContents(_fileContentsOrPath);
    }

    private bool IsFileFromServer { get; }

    public ObservableCollection<IAuthorizedKey> AuthorizedKeys
    {
        get => _authorizedKeys;
        set => this.RaiseAndSetIfChanged(ref _authorizedKeys, value);
    }

    public bool AddAuthorizedKey(ISshKey key)
    {
        if (AuthorizedKeys.Any(e => e.Fingerprint == key.Fingerprint)) return false;
        var export = key.ExportKey();
        if (export is null) return false;
        AuthorizedKeys.Add(new AuthorizedKey(export));
        return true;
    }

    public bool ApplyChanges(IEnumerable<IAuthorizedKey> keys)
    {
        var countBefore = AuthorizedKeys.Count;
        AuthorizedKeys = new ObservableCollection<IAuthorizedKey>(keys.Where(e => !e.MarkedForDeletion));
        return countBefore != AuthorizedKeys.Count;
    }

    public IAuthorizedKeysFile PersistChangesInFile()
    {
        if (IsFileFromServer) return this;
        using var fileStream = File.Open(_fileContentsOrPath, FileMode.Truncate);
        fileStream.Write(Encoding.Default.GetBytes(ExportFileContent()));
        fileStream.Close();
        ReadAndLoadFileContents(_fileContentsOrPath);
        return this;
    }

    public async Task<bool> AddAuthorizedKeyAsync(ISshKey key)
    {
        if (AuthorizedKeys.Any(e => e.Fingerprint == key.Fingerprint)) return false;
        var export = await key.ExportKeyAsync();
        if (export is null) return false;
        AuthorizedKeys.Add(new AuthorizedKey(export));
        return true;
    }

    public bool RemoveAuthorizedKey(ISshKey key)
    {
        if (AuthorizedKeys.All(e => e.Fingerprint != key.Fingerprint)) return false;
        {
            AuthorizedKeys.Remove(AuthorizedKeys.First(e => e.Fingerprint == key.Fingerprint));
            return true;
        }
    }

    public string ExportFileContent(bool local = true, PlatformID? platform = null)
    {
        return local
            ? AuthorizedKeys.Where(e => !e.MarkedForDeletion)
                .Aggregate("", (s, key) => s += $"{key.GetFullKeyEntry}\r\n")
            : AuthorizedKeys.Where(e => !e.MarkedForDeletion).Aggregate("",
                (s, key) => s +=
                    $"{key.GetFullKeyEntry}{((platform ??= Environment.OSVersion.Platform) != PlatformID.Unix ? "`r`n" : "\r\n")}");
    }

    private void LoadFileContents(string fileContents)
    {
        var splittedContents =fileContents
            .Split("\r\n", StringSplitOptions.RemoveEmptyEntries)
            .Where(e => !string.IsNullOrWhiteSpace(e.Trim()));
        AuthorizedKeys =  new ObservableCollection<IAuthorizedKey>(splittedContents.Select(e => new AuthorizedKey(e.Trim())));
    }

    private void ReadAndLoadFileContents(string pathToFile)
    {
        if (!File.Exists(pathToFile)) File.Create(pathToFile);
        using var fileStream = File.OpenRead(pathToFile);
        using var streamReader = new StreamReader(fileStream);
        LoadFileContents(streamReader.ReadToEnd());
    }
}