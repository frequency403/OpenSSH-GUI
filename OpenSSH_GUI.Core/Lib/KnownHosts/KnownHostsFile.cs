using System.Collections.ObjectModel;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Interfaces.KnownHosts;
using ReactiveUI;

namespace OpenSSH_GUI.Core.Lib.KnownHosts;

/// Represents a known hosts file.
/// /
public class KnownHostsFile : ReactiveObject, IKnownHostsFile
{
    /// Represents the path to the known hosts file.
    /// /
    private string _fileKnownHostsPath = "";

    /// <summary>
    ///     Gets or sets a boolean value indicating whether the `KnownHostsFile` object is created from a server or not.
    /// </summary>
    private bool _isFromServer;

    /// <summary>
    ///     Initializes a new instance of the <see cref="KnownHostsFile"/> class.
    /// </summary>
    public KnownHostsFile()
    {
    }

    /// <summary>
    ///     Represents a known hosts file that stores information about trusted hosts.
    /// </summary>
    /// <param name="knownHostsPathOrContent">The path to the file or its content.</param>
    /// <param name="fromServer">Indicates whether the content is from a server.</param>
    public KnownHostsFile(string knownHostsPathOrContent, bool fromServer = false)
    {
        _isFromServer = fromServer;
        if (_isFromServer)
        {
            SetKnownHosts(knownHostsPathOrContent);
        }
        else
        {
            _fileKnownHostsPath = knownHostsPathOrContent;
            // Synchronous reading is deprecated. Use InitializeAsync.
        }
    }

    /// <summary>
    ///     Initializes the known hosts file asynchronously.
    /// </summary>
    /// <param name="knownHostsPathOrContent">The path to the file or its content.</param>
    /// <param name="fromServer">Indicates whether the content is from a server.</param>
    /// <param name="token">A cancellation token.</param>
    /// <returns>A <see cref="ValueTask{IKnownHostsFile}"/> representing the initialized object.</returns>
    public async ValueTask<IKnownHostsFile> InitializeAsync(string knownHostsPathOrContent, bool fromServer = false, CancellationToken token = default)
    {
        _isFromServer = fromServer;
        if (_isFromServer)
        {
            SetKnownHosts(knownHostsPathOrContent);
        }
        else
        {
            _fileKnownHostsPath = knownHostsPathOrContent;
            await ReadContentAsync();
        }
        return this;
    }

    /// <summary>
    ///     Represents a file that contains known SSH hosts and their keys.
    /// </summary>
    public static string LineEnding { get; set; } = "\r\n";

    /// <summary>
    ///     Represents a known hosts file.
    /// </summary>
    public ObservableCollection<IKnownHost> KnownHosts
    {
        get;
        private set => this.RaiseAndSetIfChanged(ref field, value);
    } = [];

    /// <summary>
    ///     Asynchronously reads the contents of the known hosts file.
    /// </summary>
    /// <param name="stream">
    ///     The file stream to read from. If null, the method reads from the file specified in the
    ///     constructor.
    /// </param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    public async ValueTask ReadContentAsync(FileStream? stream = null)
    {
        if (_isFromServer) return;
        if (stream is null)
        {
            if (string.IsNullOrEmpty(_fileKnownHostsPath)) return;
            await using var file = new FileStream(_fileKnownHostsPath, FileMode.OpenOrCreate);
            using var streamReader = new StreamReader(file, leaveOpen: true);
            SetKnownHosts(await streamReader.ReadToEndAsync());
        }
        else
        {
            using var streamReader = new StreamReader(stream);
            SetKnownHosts(await streamReader.ReadToEndAsync());
        }
    }

    /// <summary>
    ///     Synchronizes the known hosts with the given list of new known hosts.
    /// </summary>
    /// <param name="newKnownHosts">The new known hosts to synchronize.</param>
    public void SyncKnownHosts(IEnumerable<IKnownHost> newKnownHosts)
    {
        KnownHosts = new ObservableCollection<IKnownHost>(newKnownHosts);
    }

    /// <summary>
    ///     Updates the content of the known hosts file asynchronously.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> representing the update operation.</returns>
    public async ValueTask UpdateFileAsync()
    {
        if (_isFromServer) return;
        if (string.IsNullOrEmpty(_fileKnownHostsPath)) return;
        await using var file = new FileStream(_fileKnownHostsPath, FileMode.Truncate);
        await using var streamWriter = new StreamWriter(file);
        var newContent = KnownHosts
            .Where(e => !e.DeleteWholeHost)
            .Aggregate("", (current, host) => current + host.GetAllEntries());
        await streamWriter.WriteAsync(newContent);
        SetKnownHosts(newContent);
    }

    /// <summary>
    ///     Retrieves the updated contents of the known hosts file.
    /// </summary>
    /// <param name="platformId">The platform ID of the server.</param>
    /// <returns>The updated contents of the known hosts file as a string.</returns>
    public string GetUpdatedContents(PlatformID platformId)
    {
        if (!_isFromServer) return "";
        LineEnding = platformId == PlatformID.Unix ? LineEnding : "`r`n";
        var newContent = KnownHosts
            .Where(e => !e.DeleteWholeHost)
            .Aggregate("", (current, host) => current + host.GetAllEntries());
        SetKnownHosts(newContent);
        return newContent;
    }

    /// <summary>
    ///     Sets the known hosts for the file.
    /// </summary>
    /// <param name="fileContent">The contents of the known hosts file.</param>
    private void SetKnownHosts(string fileContent)
    {
        KnownHosts = new ObservableCollection<IKnownHost>(fileContent
            .Split(LineEnding)
            .Where(e => !string.IsNullOrEmpty(e))
            .GroupBy(e => e.Split(' ')[0])
            .Select(e => new KnownHost(e)));
    }

}