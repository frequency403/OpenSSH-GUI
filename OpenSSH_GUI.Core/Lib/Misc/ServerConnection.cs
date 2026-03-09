using System.Diagnostics.CodeAnalysis;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Interfaces.AuthorizedKeys;
using OpenSSH_GUI.Core.Interfaces.Credentials;
using OpenSSH_GUI.Core.Interfaces.KnownHosts;
using OpenSSH_GUI.Core.Interfaces.Misc;
using OpenSSH_GUI.Core.Lib.AuthorizedKeys;
using OpenSSH_GUI.Core.Lib.Credentials;
using OpenSSH_GUI.Core.Lib.Keys;
using OpenSSH_GUI.Core.Lib.KnownHosts;
using ReactiveUI;
using Renci.SshNet;

namespace OpenSSH_GUI.Core.Lib.Misc;

public class ServerConnection : ReactiveObject, IServerConnection
{
    private SshClient _sshClient;

    public ServerConnection(IConnectionCredentials? credentials = null)
    {
        credentials ??= new PasswordConnectionCredentials("123", "123", "123");
        ConnectionCredentials = credentials;
        _sshClient = new SshClient(credentials.GetConnectionInfo()) { KeepAliveInterval = TimeSpan.FromSeconds(10) };
        ConnectionTime = DateTime.Now;
    }

    public ServerConnection(string hostname, string user, string password) : this(
        new PasswordConnectionCredentials(hostname.Trim(), user.Trim(), password.Trim()))
    {
    }

    public ServerConnection(string hostname, string user, SshKeyFile key) : this(
        new KeyConnectionCredentials(hostname.Trim(), user.Trim(), key))
    {
    }

    public ServerConnection(string hostname, string user, IEnumerable<SshKeyFile> keys) : this(
        new MultiKeyConnectionCredentials(hostname.Trim(), user.Trim(), keys))
    {
    }

    private SshClient ClientConnection
    {
        get => _sshClient;
        set => this.RaiseAndSetIfChanged(ref _sshClient, value);
    }

    private string ReadContentsCommand => ServerOs == PlatformID.Win32NT ? "type" : "cat";
    private string CreateEmptyFileCommand => ServerOs == PlatformID.Win32NT ? "echo. >" : "touch";
    public IConnectionCredentials ConnectionCredentials { get; }


    public DateTime ConnectionTime
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = DateTime.Now;

    public bool IsConnected
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string ConnectionString =>
        IsConnected ? $"{ConnectionCredentials.Username}@{ConnectionCredentials.Hostname}" : "";

    public PlatformID ServerOs { get; set; } = PlatformID.Other;

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
    void IDisposable.Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public async ValueTask<bool> ConnectToServerAsync(CancellationToken token = default)
    {
        if (ConnectionCredentials is IMultiKeyConnectionCredentials mkcc) return await TestMultiAsync(mkcc, token);
        await ClientConnection.ConnectAsync(token);
        IsConnected = ClientConnection.IsConnected;
        if (!IsConnected) return ServerOs != PlatformID.Other && IsConnected;
        ServerOs = await GetServerOsAsync(token);
        await CheckForFilesAndCreateThemIfTheyNotExistAsync(token);
        ConnectionTime = DateTime.Now;
        return ServerOs != PlatformID.Other && IsConnected;
    }

    public async ValueTask<bool> DisconnectFromServerAsync(CancellationToken token = default)
    {
        try
        {
            await Task.Run(() => ClientConnection.Disconnect(), token);
            IsConnected = false;
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async ValueTask<bool> CloseConnectionAsync(CancellationToken token = default)
    {
        try
        {
            await Task.Run(() => ClientConnection.Disconnect(), token);
            IsConnected = false;
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async ValueTask<IKnownHostsFile> GetKnownHostsFromServerAsync(CancellationToken token = default)
    {
        if (!IsConnected) return new KnownHostsFile("", true);

        var path = await ResolveRemoteEnvVariablesAsync(SshConfigFiles.Known_Hosts.GetPathOfFile(false, ServerOs), token);
        var command = ClientConnection.CreateCommand($"{ReadContentsCommand} {path}");
        var result = await Task.Run(() => command.Execute(), token);

        return new KnownHostsFile(result, true);
    }

    public async ValueTask<bool> WriteKnownHostsToServerAsync(IKnownHostsFile knownHostsFile, CancellationToken token = default)
    {
        if (!IsConnected) return false;

        var path = await ResolveRemoteEnvVariablesAsync(SshConfigFiles.Known_Hosts.GetPathOfFile(false, ServerOs), token);
        var command = ClientConnection.CreateCommand($"echo \"{knownHostsFile.GetUpdatedContents(ServerOs)}\" > {path}");
        var result = await Task.Run(() => command.Execute(), token);

        return command.ExitStatus == 0;
    }

    public async ValueTask<IAuthorizedKeysFile> GetAuthorizedKeysFromServerAsync(CancellationToken token = default)
    {
        if (!IsConnected) return await new AuthorizedKeysFile().InitializeAsync("", true, token);

        var path = await ResolveRemoteEnvVariablesAsync(SshConfigFiles.Authorized_Keys.GetPathOfFile(false, ServerOs), token);
        var command = ClientConnection.CreateCommand($"{ReadContentsCommand} {path}");
        var result = await Task.Run(() => command.Execute(), token);

        return await new AuthorizedKeysFile().InitializeAsync(result, true, token);
    }

    public async ValueTask<bool> WriteAuthorizedKeysChangesToServerAsync(IAuthorizedKeysFile authorizedKeysFile, CancellationToken token = default)
    {
        if (!IsConnected) return false;

        var path = await ResolveRemoteEnvVariablesAsync(SshConfigFiles.Authorized_Keys.GetPathOfFile(false, ServerOs), token);
        var command = ClientConnection.CreateCommand($"echo \"{authorizedKeysFile.ExportFileContent(false, ServerOs)}\" > {path}");
        await Task.Run(() => command.Execute(), token);

        return command.ExitStatus == 0;
    }

    public async ValueTask<bool> TestAndOpenConnectionAsync(CancellationToken token = default)
    {
        if (ConnectionCredentials is IMultiKeyConnectionCredentials mkcc) return await TestMultiAsync(mkcc, token);
        try
        {
            await ClientConnection.ConnectAsync(token);
            IsConnected = ClientConnection.IsConnected;
            if (IsConnected)
            {
                ServerOs = await GetServerOsAsync(token);
                await CheckForFilesAndCreateThemIfTheyNotExistAsync(token);
                ConnectionTime = DateTime.Now;
            }

            if (ServerOs != PlatformID.Other) return IsConnected;
            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private async ValueTask<bool> TestMultiAsync(IMultiKeyConnectionCredentials mkcc, CancellationToken token = default)
    {
        //var workingKeys = new List<SshKeyFile>();
        foreach (var key in mkcc.Keys!)
            try
            {
                // using var connection = new SshClient(mkcc.Hostname, mkcc.Username, key.GetSshNetKeyType());
                // await connection.ConnectAsync(token);
                // if (connection.IsConnected) workingKeys.Add(key);
            }
            catch (Exception)
            {
                //
            }

        //mkcc.Keys = workingKeys;
        if (mkcc.Keys.Any())
        {
            await ClientConnection.ConnectAsync(token);
            IsConnected = ClientConnection.IsConnected;
        }

        if (!IsConnected) return IsConnected;
        ServerOs = await GetServerOsAsync(token);
        await CheckForFilesAndCreateThemIfTheyNotExistAsync(token);
        ConnectionTime = DateTime.Now;
        return ServerOs != PlatformID.Other && IsConnected;
    }

    private async ValueTask<string> ResolveRemoteEnvVariablesAsync(string originalPath, CancellationToken token = default)
    {
        if (!IsConnected) return originalPath;
        var parts = originalPath.Split('%', StringSplitOptions.RemoveEmptyEntries);
        var result = "";
        foreach (var part in parts)
        {
            if (part.Contains('\\') || part.Contains('/'))
            {
                result += part.Trim();
            }
            else
            {
                var cmdText = ServerOs is PlatformID.Unix or PlatformID.MacOSX ? $"echo ${part}" : $"echo %{part}%";
                var command = ClientConnection.CreateCommand(cmdText);
                var output = await Task.Run(() => command.Execute(), token);
                result += output.Trim();
            }
        }

        return result;
    }

    private async ValueTask CheckForFilesAndCreateThemIfTheyNotExistAsync(CancellationToken token = default)
    {
        if (!ClientConnection.IsConnected) return;

        var authKeyPath = SshConfigFiles.Authorized_Keys.GetPathOfFile(false);
        var knownHostPath = SshConfigFiles.Known_Hosts.GetPathOfFile(false);

        var authorizedKeysFileCheck = ClientConnection.CreateCommand($"{ReadContentsCommand} {authKeyPath}");
        await Task.Run(() => authorizedKeysFileCheck.Execute(), token);

        var knownHostsFileCheck = ClientConnection.CreateCommand($"{ReadContentsCommand} {knownHostPath}");
        await Task.Run(() => knownHostsFileCheck.Execute(), token);

        if (authorizedKeysFileCheck.ExitStatus != 0)
        {
            var createAuthCmd = ClientConnection.CreateCommand($"{CreateEmptyFileCommand} {authKeyPath}");
            await Task.Run(() => createAuthCmd.Execute(), token);
        }

        if (knownHostsFileCheck.ExitStatus != 0)
        {
            var createKnownCmd = ClientConnection.CreateCommand($"{CreateEmptyFileCommand} {knownHostPath}");
            await Task.Run(() => createKnownCmd.Execute(), token);
        }
    }

    private async ValueTask<PlatformID> GetServerOsAsync(CancellationToken token = default)
    {
        var linuxCommand = ClientConnection.CreateCommand("uname -s");
        var windowsCommand = ClientConnection.CreateCommand("ver");

        await Task.Run(() => linuxCommand.Execute(), token);
        await Task.Run(() => windowsCommand.Execute(), token);

        var isWindows = windowsCommand.ExitStatus == 0;
        var isLinux = linuxCommand.ExitStatus == 0;

        if (isWindows && !isLinux) return PlatformID.Win32NT;
        if (isLinux && !isWindows) return PlatformID.Unix;
        return PlatformID.Other;
    }
}