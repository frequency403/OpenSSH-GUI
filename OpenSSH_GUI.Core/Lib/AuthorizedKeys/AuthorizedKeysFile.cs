using System.Collections.ObjectModel;
using OpenSSH_GUI.Core.Interfaces.AuthorizedKeys;
using OpenSSH_GUI.Core.Lib.Keys;
using ReactiveUI;

namespace OpenSSH_GUI.Core.Lib.AuthorizedKeys;

/// <summary>
///     Represents an Authorized Keys file.
/// </summary>
public class AuthorizedKeysFile : ReactiveObject, IAuthorizedKeysFile
{
    /// <summary>
    ///     The contents of the authorized keys file or the path to the file.
    /// </summary>
    private string _fileContentsOrPath;

    /// <summary>
    ///     Represents an authorized keys file.
    /// </summary>


    public async ValueTask<IAuthorizedKeysFile> InitializeAsync(string fileContentsOrPath, bool fromServer = false, CancellationToken cancellationToken = default)
    {
        IsFileFromServer = fromServer;
        _fileContentsOrPath = fileContentsOrPath;
        if (IsFileFromServer)
            LoadFileContents(_fileContentsOrPath);
        else
            await ReadAndLoadFileContents(_fileContentsOrPath, cancellationToken);
        return this;
    }

    /// <summary>
    ///     Gets a value indicating whether the file is from a server.
    /// </summary>
    private bool IsFileFromServer { get; set; }

    /// <summary>
    ///     Represents the authorized keys file.
    /// </summary
    public ObservableCollection<IAuthorizedKey> AuthorizedKeys
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = [];

    /// <summary>
    ///     Adds an authorized key to the authorized keys file.
    /// </summary>
    /// <param name="key">The SSH key to be added.</param>
    /// <returns>True if the key was successfully added, otherwise false.</returns>
    public bool AddAuthorizedKey(SshKeyFile key)
    {
        if (AuthorizedKeys.Any(e => e.Fingerprint == key.Fingerprint())) return false;
        // var export = key.ExportAuthorizedKeyEntry();
        // AuthorizedKeys.Add(new AuthorizedKey(export)); @TODO
        return true;
    }

    /// <summary>
    ///     Applies the changes to the authorized keys file.
    /// </summary>
    /// <param name="keys">The collection of keys to be applied as changes.</param>
    /// <returns>True if any changes were made to the authorized keys file; otherwise, false.</returns>
    public bool ApplyChanges(IEnumerable<IAuthorizedKey> keys)
    {
        var countBefore = AuthorizedKeys.Count;
        AuthorizedKeys = new ObservableCollection<IAuthorizedKey>(keys.Where(e => !e.MarkedForDeletion));
        return countBefore != AuthorizedKeys.Count;
    }

    /// <summary>
    ///     Persists the changes made to the authorized keys file.
    /// </summary>
    /// <returns>The modified <see cref="IAuthorizedKeysFile" /> object.</returns>
    public async ValueTask<IAuthorizedKeysFile> PersistChangesInFileAsync(CancellationToken token = default)
    {
        if (IsFileFromServer) return this;
        await using var file = new FileStream(_fileContentsOrPath, FileMode.Truncate);
        await using var streamWriter = new StreamWriter(file);
        await streamWriter.WriteAsync(ExportFileContent());
        await ReadAndLoadFileContents(_fileContentsOrPath, token);
        return this;
    }

    /// <summary>
    ///     Adds an authorized key asynchronously.
    /// </summary>
    /// <param name="key">The SSH key to be added.</param>
    /// <returns>
    ///     A task representing the asynchronous operation. The task result is a boolean value indicating whether the key
    ///     was added successfully.
    /// </returns>
    public Task<bool> AddAuthorizedKeyAsync(SshKeyFile key)
    {
        return Task.FromResult(AddAuthorizedKey(key));
    }

    /// <summary>
    ///     Removes the specified SSH key from the authorized keys list.
    /// </summary>
    /// <param name="key">The SSH key to remove.</param>
    /// <returns>
    ///     Returns <c>true</c> if the key is successfully removed;
    ///     otherwise, <c>false</c>.
    /// </returns>
    public bool RemoveAuthorizedKey(SshKeyFile key)
    {
        if (AuthorizedKeys.All(e => e.Fingerprint != key.Fingerprint())) return false;
        {
            AuthorizedKeys.Remove(AuthorizedKeys.First(e => e.Fingerprint == key.Fingerprint()));
            return true;
        }
    }

    /// <summary>
    ///     Exports the content of the authorized keys file.
    /// </summary>
    /// <param name="local">Indicates whether to export for the local machine or remote server. Default is true (local).</param>
    /// <param name="platform">
    ///     The platform ID of the server. If null, the current OS platform will be used. Only applicable if
    ///     'local' is set to false.
    /// </param>
    /// <returns>The content of the authorized keys file as a string.</returns>
    public string ExportFileContent(bool local = true, PlatformID? platform = null)
    {
        return local
            ? AuthorizedKeys.Where(e => !e.MarkedForDeletion)
                .Aggregate("", (s, key) => s += $"{key.GetFullKeyEntry}\r\n")
            : AuthorizedKeys.Where(e => !e.MarkedForDeletion).Aggregate("",
                (s, key) => s +=
                    $"{key.GetFullKeyEntry}{((platform ??= Environment.OSVersion.Platform) != PlatformID.Unix ? "`r`n" : "\r\n")}");
    }

    /// <summary>
    ///     Loads the contents of a file and parses them into a collection of authorized keys.
    /// </summary>
    /// <param name="fileContents">The contents of the file.</param>
    private void LoadFileContents(string fileContents)
    {
        var splittedContents = fileContents
            .Split("\r\n", StringSplitOptions.RemoveEmptyEntries)
            .Where(e => !string.IsNullOrWhiteSpace(e.Trim()));
        AuthorizedKeys =
            new ObservableCollection<IAuthorizedKey>(splittedContents.Select(e => new AuthorizedKey(e.Trim())));
    }

    /// <summary>
    ///     Reads and loads the contents of a file.
    /// </summary>
    /// <param name="pathToFile">The path to the file to be read and loaded.</param>
    private async ValueTask ReadAndLoadFileContents(string pathToFile, CancellationToken cancellationToken = default)
    {
        await using var fileStream = File.Open(pathToFile, FileMode.OpenOrCreate);
        using var streamReader = new StreamReader(fileStream);
        LoadFileContents(await streamReader.ReadToEndAsync(cancellationToken));
    }
}