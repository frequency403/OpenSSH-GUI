using System.Collections.ObjectModel;
using System.Text;
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
    private AuthorizedKey[] _authorizedKeys = [];

    /// <summary>
    ///     The contents of the authorized keys file or the path to the file.
    /// </summary>
    private string _fileContentsOrPath = string.Empty;

    /// <summary>
    ///     Represents an authorized keys file.
    /// </summary>
    private AuthorizedKeysFile() { }

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

    public bool ChangesMade => !_authorizedKeys.SequenceEqual(AuthorizedKeys);

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
    ///     Persists the changes made to the authorized keys file.
    /// </summary>
    /// <returns>The modified <see cref="AuthorizedKeysFile" /> object.</returns>
    public async ValueTask<AuthorizedKeysFile> PersistChangesInFileAsync(CancellationToken token = default)
    {
        if (!ChangesMade) return this;
        if (IsFileFromServer) return this;
        await using (var file = new FileStream(_fileContentsOrPath, FileMode.Truncate))
        await using (var streamWriter = new StreamWriter(file))
        {
            ReadOnlyMemory<char> content = ExportFileContent().ToCharArray();
            await streamWriter.WriteAsync(content, token);
        }

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
    public ValueTask<bool> AddAuthorizedKeyAsync(SshKeyFile key) => ValueTask.FromResult(AddAuthorizedKey(key));

    /// <summary>
    ///     Exports the content of the authorized keys file.
    /// </summary>
    /// <param name="platform">
    ///     The platform ID of the server. If null, the current OS platform will be used
    /// </param>
    /// <returns>The content of the authorized keys file as a string.</returns>
    public string ExportFileContent(PlatformID? platform = null)
    {
        var builder = new StringBuilder();
        foreach (var authorizedKey in AuthorizedKeys.Where(e => !e.MarkedForDeletion))
        {
            builder.Append($"{authorizedKey}{(platform ?? Environment.OSVersion.Platform).GetLineSeparator()}");
        }
        return builder.ToString();
    }

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
        AuthorizedKeys.Clear();
        using var streamReader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        while (await streamReader.ReadLineAsync(cancellationToken) is { } line)
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed[0] == '#')
                continue;
            AuthorizedKeys.Add(AuthorizedKey.Parse(trimmed));
        }

        if (stream is FileStream fileStream)
        {
            _fileContentsOrPath = fileStream.Name;
            IsFileFromServer = false;
        }
        else
        {
            IsFileFromServer = true;
        }
        _authorizedKeys = AuthorizedKeys.ToArray();
    }

    /// <summary>
    ///     Reads and loads the contents of a file.
    /// </summary>
    /// <param name="pathToFile">The path to the file to be read and loaded.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    private async ValueTask ReadAndLoadFileContents(string pathToFile, CancellationToken cancellationToken = default)
    {
        await using var fileStream = File.Open(pathToFile, FileMode.OpenOrCreate);
        await LoadFromStreamAsync(fileStream, cancellationToken);
    }
}