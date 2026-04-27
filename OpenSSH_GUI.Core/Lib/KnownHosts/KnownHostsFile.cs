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
    ///     Represents a known hosts file.
    /// </summary>
    [ReactiveCollection] private ObservableCollection<KnownHost> _knownHosts = [];

    private static FileStreamOptions CreateOptions()
    {
        var options = new FileStreamOptions
        {
            BufferSize = 0,
            Access = FileAccess.ReadWrite,
            Mode = FileMode.OpenOrCreate,
            Share = FileShare.ReadWrite
        };

        if (!OperatingSystem.IsWindows())
        {
            options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        }

        return options;
    }
    
    /// Represents a known hosts file.
    public KnownHostsFile(bool IsFromServer = false)
    {
        this.IsFromServer = IsFromServer;
    }

    public static KnownHostsFile Empty { get; } = new();
    public bool IsFromServer { get; init; }

    public static ValueTask<KnownHostsFile> InitializeAsync(FileInfo fileInfo, bool fromServer = false,
        CancellationToken token = default) => fileInfo is null
        ? throw new ArgumentNullException(nameof(fileInfo))
        : InitializeAsync(new FileStream(fileInfo.FullName, CreateOptions()), fromServer, true, token);

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
        if (IsFromServer) return;
        if (stream is null)
        {
            await using var file = new FileStream(SshConfigFiles.Known_Hosts.GetPathOfFile(), CreateOptions());
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
    ///     Updates the content of the known hosts file asynchronously.
    /// </summary>
    /// <returns>A <see cref="ValueTask" /> representing the update operation.</returns>
    public async ValueTask UpdateFileAsync()
    {
        if (!KnownHosts.Any(e => e.ChangesMade)) return;
        if (IsFromServer) return;
        await using var file = new FileStream(SshConfigFiles.Known_Hosts.GetPathOfFile(), FileMode.Truncate);
        await using var streamWriter = new StreamWriter(file);
        var newContent = Export();
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
        if (!IsFromServer) return string.Empty;
        var content = Export(platformId);

        using var memoryStream = new MemoryStream();
        Memory<byte> newContent = Encoding.UTF8.GetBytes(content);
        await memoryStream.WriteAsync(newContent);
        memoryStream.Seek(0, SeekOrigin.Begin);
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
            if (line.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries) is not { Length: >= 2 } splitted)
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

    private string Export(PlatformID? platformId = null)
    {
        platformId ??= Environment.OSVersion.Platform;
        var stringBuilder = new StringBuilder();
        foreach (var knownHost in KnownHosts)
        {
            stringBuilder.Append(knownHost.Export(platformId));
        }
        return stringBuilder.ToString();
    }

    public void Deconstruct(out bool IsFromServer) { IsFromServer = this.IsFromServer; }
}