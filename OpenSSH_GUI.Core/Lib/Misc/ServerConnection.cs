using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
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

    [Reactive(SetModifier = AccessModifier.Private)]
    private PlatformID _serverOs = PlatformID.Other;

    public ServerConnection(ConnectionCredentials? credentials = null)
    {
        ConnectionCredentials = credentials ?? new PasswordConnectionCredentials("123", "123", "123");
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
    void IDisposable.Dispose()
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
        var knownHostsFile = new KnownHostsFile();
        if (!IsConnected) throw new InvalidOperationException("No connection to get known hosts from");

        var path = await ResolveRemoteEnvVariablesAsync(SshConfigFiles.Known_Hosts.GetPathOfFile(false, ServerOs),
            token);
        using var command = ClientConnection.CreateCommand($"{ReadContentsCommand} {path}");
        await command.ExecuteAsync(token);
        return await knownHostsFile.InitializeAsync(command.OutputStream, true, false, token);
    }

    public async ValueTask<bool> WriteKnownHostsToServerAsync(KnownHostsFile knownHostsFile,
        CancellationToken token = default)
    {
        if (!IsConnected) return false;

        var path = await ResolveRemoteEnvVariablesAsync(SshConfigFiles.Known_Hosts.GetPathOfFile(false, ServerOs),
            token);
        using var command =
            ClientConnection.CreateCommand(
                $"echo \"{await knownHostsFile.GetUpdatedContentsAsync(ServerOs)}\" > {path}");
        await command.ExecuteAsync(token);
        return command.ExitStatus == 0;
    }

    public async ValueTask<AuthorizedKeysFile> GetAuthorizedKeysFromServerAsync(CancellationToken token = default)
    {
        if (!IsConnected)
            throw new InvalidOperationException("No connection to get authorized keys from");

        var path = await ResolveRemoteEnvVariablesAsync(SshConfigFiles.Authorized_Keys.GetPathOfFile(false, ServerOs),
            token);
        using var command = ClientConnection.CreateCommand($"{ReadContentsCommand} {path}");
        await command.ExecuteAsync(token);
        return await AuthorizedKeysFile.ParseAsync(command.OutputStream, token);
    }

    public async ValueTask<bool> WriteAuthorizedKeysChangesToServerAsync(AuthorizedKeysFile authorizedKeysFile,
        CancellationToken token = default)
    {
        if (!IsConnected) return false;

        var path = await ResolveRemoteEnvVariablesAsync(SshConfigFiles.Authorized_Keys.GetPathOfFile(false, ServerOs),
            token);
        using var command =
            ClientConnection.CreateCommand(
                $"echo \"{authorizedKeysFile.ExportFileContent(false, ServerOs)}\" > {path}");
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
                var cmdText = ServerOs is PlatformID.Unix or PlatformID.MacOSX ? $"echo ${part}" : $"echo %{part}%";
                var command = ClientConnection.CreateCommand(cmdText);
                await command.ExecuteAsync(token);
                var output = command.Result;
                result += output.Trim();
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
        using var linuxCommand = ClientConnection.CreateCommand("uname -s");
        using var windowsCommand = ClientConnection.CreateCommand("ver");

        await linuxCommand.ExecuteAsync(token);
        await windowsCommand.ExecuteAsync(token);

        var isWindows = windowsCommand.ExitStatus == 0;
        var isLinux = linuxCommand.ExitStatus == 0;

        if (isWindows && !isLinux) return PlatformID.Win32NT;
        if (isLinux && !isWindows) return PlatformID.Unix;
        return PlatformID.Other;
    }
}