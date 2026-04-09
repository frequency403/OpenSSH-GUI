using System.Collections.ObjectModel;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Lib.Keys;
using ReactiveUI;

namespace OpenSSH_GUI.Core.Lib.AuthorizedKeys;

/// <summary>
///     Represents an Authorized Keys file.
/// </summary>
public class AuthorizedKeysFile : ReactiveObject
{
    /// <summary>
    ///     The contents of the authorized keys file or the path to the file.
    /// </summary>
    private string _fileContentsOrPath = string.Empty;

    /// <summary>
    ///     Represents an authorized keys file.
    /// </summary>
    private AuthorizedKeysFile()
    {
    }

    /// <summary>
    ///     Gets a value indicating whether the file is from a server.
    /// </summary>
    private bool IsFileFromServer { get; set; }

    /// <summary>
    ///     Represents the authorized keys file.
    /// </summary>
    public ObservableCollection<AuthorizedKey> AuthorizedKeys
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = [];

    public static AuthorizedKeysFile Empty { get; } = new();

    public bool CanAddKey(SshKeyFile key)
    {
        try
        {
            return !AuthorizedKeys.Any(e => e.Equals(key.AuthorizedKey));
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    ///     Adds an authorized key to the authorized keys file.
    /// </summary>
    /// <param name="key">The SSH key to be added.</param>
    /// <returns>True if the key was successfully added, otherwise false.</returns>
    public bool AddAuthorizedKey(SshKeyFile key)
    {
        if (AuthorizedKeys.Any(e => e.Equals(key.AuthorizedKey))) return false;
        AuthorizedKeys.Add(key.AuthorizedKey);
        return true;
    }

    /// <summary>
    ///     Applies the changes to the authorized keys file.
    /// </summary>
    /// <param name="keys">The collection of keys to be applied as changes.</param>
    /// <returns>True if any changes were made to the authorized keys file; otherwise, false.</returns>
    public bool ApplyChanges(IEnumerable<AuthorizedKey> keys)
    {
        var countBefore = AuthorizedKeys.Count;
        AuthorizedKeys = new ObservableCollection<AuthorizedKey>(keys.Where(e => !e.MarkedForDeletion));
        return countBefore != AuthorizedKeys.Count;
    }

    /// <summary>
    ///     Persists the changes made to the authorized keys file.
    /// </summary>
    /// <returns>The modified <see cref="IAuthorizedKeysFile" /> object.</returns>
    public async ValueTask<AuthorizedKeysFile> PersistChangesInFileAsync(CancellationToken token = default)
    {
        if (IsFileFromServer) return this;
        await using (var file = new FileStream(_fileContentsOrPath, FileMode.Truncate))
        await using (var streamWriter = new StreamWriter(file))
            await streamWriter.WriteAsync(ExportFileContent());
        await ReadAndLoadFileContents(_fileContentsOrPath, token);
        return this;
    }

    /// <summary>
    ///     Adds an authorized key asynchronously.
    /// </summary>
    /// <param name="key">The SSH key to be added.</param>
    /// <returns>
    ///     A <see cref="ValueTask{Boolean}" /> indicating whether the key was added successfully.
    /// </returns>
    public ValueTask<bool> AddAuthorizedKeyAsync(SshKeyFile key)
    {
        return ValueTask.FromResult(AddAuthorizedKey(key));
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
        if (AuthorizedKeys.All(e => e.Fingerprint != key.Fingerprint)) return false;
        {
            AuthorizedKeys.Remove(AuthorizedKeys.First(e => e.Fingerprint == key.Fingerprint));
            return true;
        }
    }

    /// <summary>
    ///     Exports the content of the authorized keys file.
    /// </summary>
    /// <param name="platform">
    ///     The platform ID of the server. If null, the current OS platform will be used. Only applicable if
    ///     'local' is set to false.
    /// </param>
    /// <returns>The content of the authorized keys file as a string.</returns>
    public string ExportFileContent(PlatformID? platform = null)
        => AuthorizedKeys.Where(e => !e.MarkedForDeletion)
            .Aggregate("",
                (s, key) => s +=
                    $"{key.GetFullKeyEntry}{((platform ?? Environment.OSVersion.Platform).GetLineSeparator())}");

    public static async ValueTask<AuthorizedKeysFile> OpenAsync(string? filePath = null,
        CancellationToken cancellationToken = default)
    {
        filePath ??= SshConfigFiles.Authorized_Keys.GetPathOfFile();
        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists)
            throw new FileNotFoundException("Authorized keyfile was not found", fileInfo.Name);
        await using var fileStream = File.Open(filePath, FileMode.OpenOrCreate);
        return await ParseAsync(fileStream, cancellationToken);
    }

    public static async ValueTask<AuthorizedKeysFile> ParseAsync(Stream stream,
        CancellationToken cancellationToken = default)
    {
        var authorizedKeyFile = new AuthorizedKeysFile();
        await authorizedKeyFile.LoadFromStreamAsync(stream, cancellationToken);
        return authorizedKeyFile;
    }

    /// <summary>
    ///     Asynchronously loads authorized keys from a given stream.
    /// </summary>
    /// <param name="stream">The stream containing the authorized keys file content.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    private async ValueTask LoadFromStreamAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        using var streamReader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        if (await streamReader.ReadToEndAsync(cancellationToken) is { } fileContents &&
            !string.IsNullOrWhiteSpace(fileContents)) LoadFileContents(fileContents);

        if (stream is FileStream fileStream)
        {
            _fileContentsOrPath = fileStream.Name;
            IsFileFromServer = false;
        }
        else
        {
            IsFileFromServer = true;
        }
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
            new ObservableCollection<AuthorizedKey>(splittedContents.Select(e => AuthorizedKey.Parse(e.Trim())));
    }

    /// <summary>
    ///     Reads and loads the contents of a file.
    /// </summary>
    /// <param name="pathToFile">The path to the file to be read and loaded.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    private async ValueTask ReadAndLoadFileContents(string pathToFile, CancellationToken cancellationToken = default)
    {
        await using var fileStream = File.Open(pathToFile, FileMode.OpenOrCreate);
        using var streamReader = new StreamReader(fileStream);
        LoadFileContents(await streamReader.ReadToEndAsync(cancellationToken));
    }
    
    // REFACTOR: Consider using a more efficient file reading approach, such as reading line by line
    // REFACTOR: Implement Export(PlatformId) method to return the contents of the file
}