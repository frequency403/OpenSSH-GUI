using System.Collections.ObjectModel;
using System.Text;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Services;
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
            : InitializeAsync(new FileStream(fileInfo.FullName, SshKeyManager.FileStreamOptions), fromServer, true, token);
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
            await using var file = new FileStream(SshConfigFiles.Known_Hosts.GetPathOfFile(), SshKeyManager.FileStreamOptions);
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
            .Aggregate("", (current, host) => current + host.Export());
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
            .Aggregate("", (current, host) => current + host.Export(platformId));

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
        var dicc = new Dictionary<KnownHostHost, KnownHostKey[]>();
        while (await streamReader.ReadLineAsync(token) is { } line)
        {
            if(line.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries) is not { Length: >= 2} splitted)
                continue;
            foreach (var host in splitted[0].Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                var uri = new KnownHostHost(host);
                var key = new KnownHostKey(splitted[1..]);
                dicc.Add(uri, dicc.Remove(uri, out var keys) ? keys.Append(key).ToArray() : [key]);
            }
        }
        foreach (var dictionaryEntry in dicc)
        {
            KnownHosts.Add(new KnownHost(dictionaryEntry));
        }
    }
}

public readonly record struct KnownHostHost
{
    private readonly string _originalHostEntry;

    public int Port { get; } = 22;
    public string Host { get; } = string.Empty;
    
    public KnownHostHost(string host)
    {
        _originalHostEntry = host;
        if (host.Split(':') is not { Length: 2 } split)
        {
            Host = host;
        }
        else
        {
            Port = int.Parse(split[1]);
            Host = split[0];
        }
        Host = Host.Trim('[', ']');
    }

    public override string ToString()
    {
        return _originalHostEntry;
    }
}