using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Text;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Lib.AuthorizedKeys;
using OpenSSH_GUI.Core.Lib.KnownHosts;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using Renci.SshNet;

namespace OpenSSH_GUI.Core.Lib.Misc;

public sealed partial class ServerConnection : ReactiveObject, IDisposable
{
    private const string RemoteSshDirectory = ".ssh";

    // SftpClient.ChangePermissions expects the octal digits written as a decimal number (e.g. 700 => rwx------).
    private const short RemoteSshDirectoryMode = 700;

    private const short RemoteSshFileMode = 600;

    private readonly CompositeDisposable _disposables = new();

    [ObservableAsProperty(ReadOnly = true)]
    private string _connectionString = string.Empty;

    [Reactive(SetModifier = AccessModifier.Private)]
    private DateTime _connectionTime = DateTime.Now;

    [Reactive(SetModifier = AccessModifier.Private)]
    private bool _isConnected;

    [ObservableAsProperty(ReadOnly = true)]
    private string _lineSeparator = string.Empty;

    [Reactive(SetModifier = AccessModifier.Private)]
    private PlatformID _serverOs = PlatformID.Other;

    private ServerConnection(ConnectionCredentials? credentials = null)
    {
        ConnectionCredentials = credentials ?? ConnectionCredentials.Empty;
        var connectionInfo = ConnectionCredentials.GetConnectionInfo();
        ClientConnection = new SshClient(connectionInfo)
        {
            KeepAliveInterval = TimeSpan.FromSeconds(10)
        };
        FileTransferConnection = new SftpClient(connectionInfo);

        _connectionStringHelper = this.WhenAnyValue(obj => obj.IsConnected)
            .Select(c => c ? $"{ConnectionCredentials.Username}@{ConnectionCredentials.Hostname}" : string.Empty)
            .ToProperty(this, obj => obj.ConnectionString)
            .DisposeWith(_disposables);

        _lineSeparatorHelper = this.WhenAnyValue(obj => obj.ServerOs)
            .Select(e => e.GetLineSeparator())
            .ToProperty(this, obj => obj.LineSeparator)
            .DisposeWith(_disposables);
    }

    public static ServerConnection Empty { get; } = new();

    private ConnectionCredentials ConnectionCredentials
    {
        get;
        init => this.RaiseAndSetIfChanged(ref field, value);
    }

    private SshClient ClientConnection
    {
        get;
        init => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>
    ///     SFTP channel used for all remote file access. Transferring file contents over SFTP avoids
    ///     building shell command lines from file paths or file contents.
    /// </summary>
    private SftpClient FileTransferConnection
    {
        get;
        init => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _disposables.Dispose();
        FileTransferConnection.Dispose();
        ClientConnection.Dispose();
    }

    public static ServerConnection WithCredentials(ConnectionCredentials credentials) => new(credentials);

    public async ValueTask<bool> ConnectToServerAsync(CancellationToken token = default)
    {
        await ClientConnection.ConnectAsync(token);
        if (ClientConnection.IsConnected)
            await FileTransferConnection.ConnectAsync(token);
        IsConnected = ClientConnection.IsConnected && FileTransferConnection.IsConnected;
        if (!IsConnected) return ServerOs != PlatformID.Other && IsConnected;
        ServerOs = await GetServerOsAsync(token);
        ConnectionTime = DateTime.Now;
        return ServerOs != PlatformID.Other && IsConnected;
    }

    public ValueTask<bool> DisconnectFromServerAsync(CancellationToken token = default)
    {
        try
        {
            FileTransferConnection.Disconnect();
            ClientConnection.Disconnect();
            IsConnected = ClientConnection.IsConnected;
            return ValueTask.FromResult(true);
        }
        catch (Exception)
        {
            return ValueTask.FromResult(false);
        }
    }

    public async ValueTask<KnownHostsFile> GetKnownHostsFromServerAsync(CancellationToken token = default)
    {
        if (!IsConnected) throw new InvalidOperationException("No connection to get known hosts from");

        var content = await ReadRemoteFileAsync(SshConfigFiles.Known_Hosts, token);
        return await KnownHostsFile.InitializeAsync(content, true, true, token);
    }

    public async ValueTask<bool> WriteKnownHostsToServerAsync(KnownHostsFile knownHostsFile,
        CancellationToken token = default)
    {
        if (!knownHostsFile.KnownHosts.Any(e => e.ChangesMade)) return false;
        if (!IsConnected) return false;

        var content = await knownHostsFile.GetUpdatedContentsAsync(ServerOs);
        await WriteRemoteFileAsync(SshConfigFiles.Known_Hosts, content, token);
        return true;
    }

    public async ValueTask<AuthorizedKeysFile> GetAuthorizedKeysFromServerAsync(CancellationToken token = default)
    {
        if (!IsConnected)
            throw new InvalidOperationException("No connection to get authorized keys from");

        await using var content = await ReadRemoteFileAsync(SshConfigFiles.Authorized_Keys, token);
        return await AuthorizedKeysFile.ParseAsync(content, token);
    }

    public async ValueTask<bool> WriteAuthorizedKeysChangesToServerAsync(AuthorizedKeysFile authorizedKeysFile,
        CancellationToken token = default)
    {
        if (!authorizedKeysFile.ChangesMade) return false;
        if (!IsConnected) return false;

        var content = authorizedKeysFile.ExportFileContent(ServerOs);
        await WriteRemoteFileAsync(SshConfigFiles.Authorized_Keys, content, token);
        return true;
    }

    /// <summary>
    ///     Returns the SFTP path of the given file inside the remote user's SSH directory.
    ///     The path is relative to the SFTP working directory, which is the user's home directory
    ///     on both OpenSSH for Unix and OpenSSH for Windows.
    /// </summary>
    private static string GetRemotePath(SshConfigFiles file) =>
        $"{RemoteSshDirectory}/{Enum.GetName(file)!.ToLowerInvariant()}";

    /// <summary>
    ///     Reads the given remote file via SFTP. A missing file yields an empty stream.
    /// </summary>
    private async ValueTask<Stream> ReadRemoteFileAsync(SshConfigFiles file, CancellationToken token)
    {
        var path = GetRemotePath(file);
        var content = new MemoryStream();
        if (await FileTransferConnection.ExistsAsync(path, token))
            await FileTransferConnection.DownloadFileAsync(path, content, token);
        content.Seek(0, SeekOrigin.Begin);
        return content;
    }

    /// <summary>
    ///     Replaces the contents of the given remote file via SFTP. Missing files and the
    ///     SSH directory are created with owner-only permissions on Unix hosts.
    /// </summary>
    private async ValueTask WriteRemoteFileAsync(SshConfigFiles file, string content, CancellationToken token)
    {
        var path = GetRemotePath(file);
        var isUnix = ServerOs is PlatformID.Unix or PlatformID.MacOSX;

        if (!await FileTransferConnection.ExistsAsync(RemoteSshDirectory, token))
        {
            await FileTransferConnection.CreateDirectoryAsync(RemoteSshDirectory, token);
            if (isUnix) FileTransferConnection.ChangePermissions(RemoteSshDirectory, RemoteSshDirectoryMode);
        }

        var isNewFile = !await FileTransferConnection.ExistsAsync(path, token);

        await using (var remoteFile =
                     await FileTransferConnection.OpenAsync(path, FileMode.Create, FileAccess.Write, token))
        {
            var bytes = new UTF8Encoding(false).GetBytes(content);
            await remoteFile.WriteAsync(bytes, token);
        }

        if (isNewFile && isUnix) FileTransferConnection.ChangePermissions(path, RemoteSshFileMode);
    }

    private async ValueTask<PlatformID> GetServerOsAsync(CancellationToken token = default)
    {
        using var unixCommand = ClientConnection.CreateCommand("uname -s");
        await unixCommand.ExecuteAsync(token);

        if (unixCommand.ExitStatus == 0)
            return PlatformID.Unix;

        using var windowsCommand = ClientConnection.CreateCommand("ver");
        await windowsCommand.ExecuteAsync(token);

        if (windowsCommand.ExitStatus == 0 &&
            windowsCommand.Result.Contains("Windows", StringComparison.OrdinalIgnoreCase))
            return PlatformID.Win32NT;

        return PlatformID.Other;
    }
}
