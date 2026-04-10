using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
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
    private readonly CompositeDisposable _disposables = new();

    [ObservableAsProperty(ReadOnly = true)]
    private string _connectionString = string.Empty;

    [Reactive(SetModifier = AccessModifier.Private)]
    private DateTime _connectionTime = DateTime.Now;

    [ObservableAsProperty(ReadOnly = true)]
    private string _createEmptyFileCommand = string.Empty;

    [Reactive(SetModifier = AccessModifier.Private)]
    private bool _isConnected;

    [ObservableAsProperty(ReadOnly = true)]
    private string _readContentsCommand = string.Empty;

    [ObservableAsProperty(ReadOnly = true)]
    private string _lineSeparator = string.Empty;

    [Reactive(SetModifier = AccessModifier.Private)]
    private PlatformID _serverOs = PlatformID.Other;

    public static ServerConnection Empty { get; } = new();
    public static ServerConnection WithCredentials(ConnectionCredentials credentials) => new(credentials);
    
    private ServerConnection(ConnectionCredentials? credentials = null)
    {
        ConnectionCredentials = credentials ?? ConnectionCredentials.Empty;
        ClientConnection = new SshClient(ConnectionCredentials.GetConnectionInfo())
            { KeepAliveInterval = TimeSpan.FromSeconds(10) };

        _connectionStringHelper = this.WhenAnyValue(obj => obj.IsConnected)
            .Select(c => c ? $"{ConnectionCredentials.Username}@{ConnectionCredentials.Hostname}" : string.Empty)
            .ToProperty(this, obj => obj.ConnectionString)
            .DisposeWith(_disposables);

        _readContentsCommandHelper = this.WhenAnyValue(obj => obj.ServerOs)
            .Select(c => c == PlatformID.Win32NT ? "type" : "cat")
            .ToProperty(this, obj => obj.ReadContentsCommand)
            .DisposeWith(_disposables);

        _createEmptyFileCommandHelper = this.WhenAnyValue(obj => obj.ServerOs)
            .Select(c => c == PlatformID.Win32NT ? "echo. >" : "touch")
            .ToProperty(this, obj => obj.CreateEmptyFileCommand)
            .DisposeWith(_disposables);

        _lineSeparatorHelper = this.WhenAnyValue(obj => obj.ServerOs)
            .Select(e => e.GetLineSeparator())
            .ToProperty(this, obj => obj.LineSeparator)
            .DisposeWith(_disposables);
    }

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

    /// <inheritdoc />
    public void Dispose()
    {
        _disposables.Dispose();
    }

    public async ValueTask<bool> ConnectToServerAsync(CancellationToken token = default)
    {
        await ClientConnection.ConnectAsync(token);
        IsConnected = ClientConnection.IsConnected;
        if (!IsConnected) return ServerOs != PlatformID.Other && IsConnected;
        ServerOs = await GetServerOsAsync(token);
        await CheckForFilesAndCreateThemIfTheyNotExistAsync(token);
        ConnectionTime = DateTime.Now;
        return ServerOs != PlatformID.Other && IsConnected;
    }

    public ValueTask<bool> DisconnectFromServerAsync(CancellationToken token = default)
    {
        try
        {
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

        var path = await ResolveRemoteEnvVariablesAsync(SshConfigFiles.Known_Hosts.GetPathOfFile(false, ServerOs), token);
        using var command = ClientConnection.CreateCommand($"{ReadContentsCommand} {path}");
        await command.ExecuteAsync(token);
        return await KnownHostsFile.InitializeAsync(command.OutputStream, true, false, token);
    }

    public async ValueTask<bool> WriteKnownHostsToServerAsync(KnownHostsFile knownHostsFile,
        CancellationToken token = default)
    {
        if (!IsConnected) return false;

        var path = await ResolveRemoteEnvVariablesAsync(
            SshConfigFiles.Known_Hosts.GetPathOfFile(false, ServerOs), token);
        var content = await knownHostsFile.GetUpdatedContentsAsync(ServerOs);
        using var command = ClientConnection.CreateCommand(BuildRemoteWriteCommand(ServerOs, content, path));
        await command.ExecuteAsync(token);
        return command.ExitStatus == 0;
    }

    public async ValueTask<AuthorizedKeysFile> GetAuthorizedKeysFromServerAsync(CancellationToken token = default)
    {
        if (!IsConnected)
            throw new InvalidOperationException("No connection to get authorized keys from");

        var path = await ResolveRemoteEnvVariablesAsync(
            SshConfigFiles.Authorized_Keys.GetPathOfFile(false, ServerOs), token);
        using var command = ClientConnection.CreateCommand($"{ReadContentsCommand} {path}");
        await command.ExecuteAsync(token);
        return await AuthorizedKeysFile.ParseAsync(command.OutputStream, token);
    }

    public async ValueTask<bool> WriteAuthorizedKeysChangesToServerAsync(AuthorizedKeysFile authorizedKeysFile,
        CancellationToken token = default)
    {
        if (!IsConnected) return false;

        var path = await ResolveRemoteEnvVariablesAsync(
            SshConfigFiles.Authorized_Keys.GetPathOfFile(false, ServerOs), token);
        var content = authorizedKeysFile.ExportFileContent(ServerOs);
        using var command = ClientConnection.CreateCommand(BuildRemoteWriteCommand(ServerOs, content, path));
        await command.ExecuteAsync(token);
        return command.ExitStatus == 0;
    }

    private async ValueTask<string> ResolveRemoteEnvVariablesAsync(string originalPath,
        CancellationToken token = default)
    {
        if (!IsConnected) return originalPath;
        var parts = originalPath.Split('%', StringSplitOptions.RemoveEmptyEntries);
        var result = "";
        foreach (var part in parts)
            if (part.Contains('\\') || part.Contains('/'))
            {
                result += part.Trim();
            }
            else
            {
                var cmdText = ServerOs is PlatformID.Unix or PlatformID.MacOSX
                    ? $"echo ${part}"
                    : $"echo %{part}%";
                using var command = ClientConnection.CreateCommand(cmdText);
                await command.ExecuteAsync(token);
                result += command.Result.Trim();
            }

        return result;
    }

    private async ValueTask CheckForFilesAndCreateThemIfTheyNotExistAsync(CancellationToken token = default)
    {
        if (!ClientConnection.IsConnected) return;

        var authKeyPath = SshConfigFiles.Authorized_Keys.GetPathOfFile(false);
        var knownHostPath = SshConfigFiles.Known_Hosts.GetPathOfFile(false);

        using var authorizedKeysFileCheck = ClientConnection.CreateCommand($"{ReadContentsCommand} {authKeyPath}");
        await authorizedKeysFileCheck.ExecuteAsync(token);

        using var knownHostsFileCheck = ClientConnection.CreateCommand($"{ReadContentsCommand} {knownHostPath}");
        await knownHostsFileCheck.ExecuteAsync(token);

        if (authorizedKeysFileCheck.ExitStatus != 0)
        {
            using var createAuthCmd = ClientConnection.CreateCommand($"{CreateEmptyFileCommand} {authKeyPath}");
            await createAuthCmd.ExecuteAsync(token);
        }

        if (knownHostsFileCheck.ExitStatus != 0)
        {
            using var createKnownCmd = ClientConnection.CreateCommand($"{CreateEmptyFileCommand} {knownHostPath}");
            await createKnownCmd.ExecuteAsync(token);
        }
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

    /// <summary>
    /// Builds a platform-appropriate shell command to write the given content to a file on the remote host.
    /// </summary>
    /// <param name="platformId">The <see cref="PlatformID"/> of the remote host.</param>
    /// <param name="content">The content to write into the file.</param>
    /// <param name="filePath">The full remote path of the target file.</param>
    /// <param name="append">If <c>true</c>, appends to the file instead of overwriting it.</param>
    /// <returns>A shell command string ready to be executed on the remote host.</returns>
    /// <exception cref="PlatformNotSupportedException">
    /// Thrown when no write command can be constructed for the given <paramref name="platformId"/>.
    /// </exception>
    private static string BuildRemoteWriteCommand(PlatformID platformId, string content, string filePath,
        bool append = false)
    {
        var redirectOperator = append ? ">>" : ">";

        return platformId is PlatformID.Unix or PlatformID.MacOSX
            ? BuildUnixCommand(content, filePath, redirectOperator)
            : BuildWindowsCommand(content, filePath, redirectOperator);
    }

    /// <summary>
    /// Builds a Unix shell write command using <c>printf</c> for reliable, escape-safe output.
    /// </summary>
    /// <param name="content">The content to write.</param>
    /// <param name="filePath">The target file path on the remote host.</param>
    /// <param name="redirectOperator">Shell redirect operator (<c>&gt;</c> or <c>&gt;&gt;</c>).</param>
    /// <returns>A Unix shell command string.</returns>
    private static string BuildUnixCommand(string content, string filePath, string redirectOperator)
    {
        var escaped = content.Replace("'", "'\\''");
        return $"printf '%s' '{escaped}' {redirectOperator} '{filePath}'";
    }

    /// <summary>
    /// Builds a Windows shell write command using PowerShell's <c>Set-Content</c> or <c>Add-Content</c>
    /// for reliable Unicode-safe file writing.
    /// </summary>
    /// <param name="content">The content to write.</param>
    /// <param name="filePath">The target file path on the remote host.</param>
    /// <param name="redirectOperator">Shell redirect operator (<c>&gt;</c> or <c>&gt;&gt;</c>), used to determine append mode.</param>
    /// <returns>A PowerShell command string.</returns>
    private static string BuildWindowsCommand(string content, string filePath, string redirectOperator)
    {
        var escaped = content.Replace("'", "''");
        var cmdlet = redirectOperator == ">>" ? "Add-Content" : "Set-Content";
        return $"powershell -Command \"{cmdlet} -Path '{filePath}' -Value '{escaped}' -NoNewline -Encoding UTF8\"";
    }
}