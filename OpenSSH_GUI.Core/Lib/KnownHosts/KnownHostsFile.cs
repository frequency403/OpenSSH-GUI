using System.Collections.ObjectModel;
using System.Text;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Extensions;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace OpenSSH_GUI.Core.Lib.KnownHosts;

/// Represents a known hosts file.
public sealed partial record KnownHostsFile : ReactiveRecord
{
    /// <summary>
    ///     Gets or sets a boolean value indicating whether the `KnownHostsFile` object is created from a server or not.
    /// </summary>
    private readonly bool _isFromServer;

    private readonly string _lineEnding;

    /// <summary>
    ///     Represents a known hosts file.
    /// </summary>
    [ReactiveCollection] private ObservableCollection<KnownHost> _knownHosts = [];
    
    private KnownHostsFile(bool isFromServer = false, PlatformID? platformId = null) 
    { 
        _isFromServer = isFromServer;
        _lineEnding = (platformId ?? Environment.OSVersion.Platform).GetLineSeparator();
    }
    public static KnownHostsFile Empty { get; } = new();

    public static ValueTask<KnownHostsFile> InitializeAsync(FileInfo fileInfo, bool fromServer = false,
        CancellationToken token = default)
    {
        return fileInfo is null
            ? throw new ArgumentNullException(nameof(fileInfo))
            : InitializeAsync(new FileStream(fileInfo.FullName, FileMode.OpenOrCreate), fromServer, true, token);
    }

    public static async ValueTask<KnownHostsFile> InitializeAsync(Stream knownHostsContent, bool fromServer = false,
        bool disposeStream = true, CancellationToken token = default)
    {
        var knownHostsFile = new KnownHostsFile(fromServer);
        if (fromServer)
            await knownHostsFile.SetKnownHostsAsync(knownHostsContent, disposeStream, token);
        else
            await knownHostsFile.ReadContentAsync(token: token);

        return knownHostsFile;
    }

    /// <summary>
    ///     Asynchronously reads the contents of the known hosts file.
    /// </summary>
    /// <param name="stream">
    ///     The file stream to read from. If null, the method reads from the file specified in the
    ///     constructor.
    /// </param>
    /// <param name="token">A cancellation token.</param>
    /// <returns>A <see cref="ValueTask" /> representing the asynchronous operation.</returns>
    public async ValueTask ReadContentAsync(FileStream? stream = null, CancellationToken token = default)
    {
        if (_isFromServer) return;
        if (stream is null)
        {
            await using var file = new FileStream(SshConfigFiles.Known_Hosts.GetPathOfFile(), FileMode.OpenOrCreate);
            using var streamReader = new StreamReader(file, leaveOpen: true);
            await SetKnownHostsAsync(file, false, token);
        }
        else
        {
            using var streamReader = new StreamReader(stream);
            await SetKnownHostsAsync(stream, token: token);
        }
    }

    /// <summary>
    ///     Synchronizes the known hosts with the given list of new known hosts.
    /// </summary>
    /// <param name="newKnownHosts">The new known hosts to synchronize.</param>
    public void SyncKnownHosts(IEnumerable<KnownHost> newKnownHosts)
    {
        KnownHosts = new ObservableCollection<KnownHost>(newKnownHosts);
    }

    /// <summary>
    ///     Updates the content of the known hosts file asynchronously.
    /// </summary>
    /// <returns>A <see cref="ValueTask" /> representing the update operation.</returns>
    public async ValueTask UpdateFileAsync()
    {
        // BUG: Only write to file when changes were made
        if (_isFromServer) return;
        await using var file = new FileStream(SshConfigFiles.Known_Hosts.GetPathOfFile(), FileMode.Truncate);
        await using var streamWriter = new StreamWriter(file);
        var newContent = KnownHosts
            .Where(e => !e.DeleteWholeHost)
            .Aggregate("", (current, host) => current + host.GetAllEntries());
        await streamWriter.WriteAsync(newContent);
        file.Seek(0, SeekOrigin.Begin);
        await SetKnownHostsAsync(file, false);
    }

    /// <summary>
    ///     Retrieves the updated contents of the known hosts file.
    /// </summary>
    /// <param name="platformId">The platform ID of the server.</param>
    /// <returns>The updated contents of the known hosts file as a string.</returns>
    public async ValueTask<string> GetUpdatedContentsAsync(PlatformID platformId)
    {
        if (!_isFromServer) return "";
        var content = KnownHosts
            .Where(e => !e.DeleteWholeHost)
            .Aggregate("", (current, host) => current + host.GetAllEntries());

        using var memoryStream = new MemoryStream();
        Memory<byte> newContent = Encoding.UTF8.GetBytes(content);
        await memoryStream.WriteAsync(newContent);
        await SetKnownHostsAsync(memoryStream, false);
        return content;
    }

    private async ValueTask SetKnownHostsAsync(Stream contentStream, bool disposeStream = true,
        CancellationToken token = default)
    {
        KnownHosts.Clear();
        using var streamReader = new StreamReader(contentStream, leaveOpen: !disposeStream);
        foreach (var knownHost in (await streamReader.ReadToEndAsync(token)).Split(_lineEnding)
                 .Where(e => !string.IsNullOrEmpty(e))
                 .GroupBy(e => e.Split(' ')[0])
                 .Select(e => new KnownHost(e, _lineEnding)))
            KnownHosts.Add(knownHost);
    }
}