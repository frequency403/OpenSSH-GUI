using System.Diagnostics.CodeAnalysis;
using OpenSSHALib.Enums;
using OpenSSHALib.Extensions;
using OpenSSHALib.Models;
using ReactiveUI;
using Renci.SshNet;

namespace OpenSSHALib.Lib;

public class ServerConnection : ReactiveObject, IDisposable
{
    private bool _isConnected;
    private SshClient _sshClient;
    
    
    private TimeOnly _connectionTime = TimeOnly.FromDateTime(DateTime.Now);

    public TimeOnly ConnectionTime
    {
        get => _connectionTime;
        set => this.RaiseAndSetIfChanged(ref _connectionTime, value);
    }
    
    public ServerConnection(string hostname, string user, string password)
    {
        Hostname = hostname;
        Username = user;
        Password = password;
        _sshClient = new SshClient(Hostname, Username, Password)
        {
            KeepAliveInterval = TimeSpan.FromSeconds(10)
        };
        ConnectionTime = TimeOnly.FromDateTime(DateTime.Now);
    }

    public ServerConnection(string hostname, string user, SshPublicKey key)
    {
        Hostname = hostname;
        Username = user;
        AuthKey = key;
        _sshClient = new SshClient(Hostname, Username, new PrivateKeyFile(AuthKey.PrivateKey.AbsoluteFilePath))
        {
            KeepAliveInterval = TimeSpan.FromSeconds(10)
        };
    }

    private string Hostname { get; }
    private string Username { get; }
    private string? Password { get; }
    private SshPublicKey? AuthKey { get; }

    private SshClient ClientConnection
    {
        get => _sshClient;
        set => this.RaiseAndSetIfChanged(ref _sshClient, value);
    }

    public bool IsConnected
    {
        get => _isConnected;
        set => this.RaiseAndSetIfChanged(ref _isConnected, value);
    }

    public string ConnectionString => IsConnected ? $"{Username}@{Hostname}" : "";
    public PlatformID ServerOs { get; set; } = PlatformID.Other;
    private string ReadContentsCommand => ServerOs == PlatformID.Win32NT ? "type" : "cat";
    private string CreateEmptyFileCommand => ServerOs == PlatformID.Win32NT ? "echo. >" : "touch";
    
    private string ResolveRemoteEnvVariables(string originalPath)
    {
        if (!IsConnected) return originalPath;
        return originalPath.Split('%', StringSplitOptions.RemoveEmptyEntries).Aggregate("", (s, s1) =>
        {
            if (s1.Contains('\\') || s1.Contains('/'))
            {
                s += s1.Trim();
            }
            else
            {
                s += ClientConnection.RunCommand(ServerOs is PlatformID.Unix or PlatformID.MacOSX ? $"echo ${s1}" : $"echo %{s1}%").Result.Trim();
            }
            return s;
        });
    }

    private void CheckForFilesAndCreateThemIfTheyNotExist()
    {
        if(!ClientConnection.IsConnected) return;
        var authorizedKeysFileCheck = ClientConnection.RunCommand($"{ReadContentsCommand} {SshConfigFiles.Authorized_Keys.GetPathOfFile(false)}");
        var knownHostsFileCheck = ClientConnection.RunCommand($"{ReadContentsCommand} {SshConfigFiles.Known_Hosts.GetPathOfFile(false)}");
        if (authorizedKeysFileCheck.ExitStatus != 0)
            ClientConnection.RunCommand(
                $"{CreateEmptyFileCommand} {SshConfigFiles.Authorized_Keys.GetPathOfFile(false)}");
        if(knownHostsFileCheck.ExitStatus != 0) ClientConnection.RunCommand($"{CreateEmptyFileCommand} {SshConfigFiles.Known_Hosts.GetPathOfFile(false)}");
    }
    
    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
    void IDisposable.Dispose()
    {
        GC.SuppressFinalize(this);
    }

    private PlatformID GetServerOs()
    {
        var linuxCommand = ClientConnection.RunCommand("uname -s");
        var windowsCommand = ClientConnection.RunCommand("ver");

        var isWindows = windowsCommand.ExitStatus == 0;
        var isLinux = linuxCommand.ExitStatus == 0;

        if (isWindows && !isLinux) return PlatformID.Win32NT;
        if (isLinux && !isWindows) return PlatformID.Unix;
        return PlatformID.Other;
    }

    public bool TestAndOpenConnection([NotNullWhen(false)] out Exception? exception)
    {
        exception = null;
        try
        {
            ClientConnection.Connect();
            IsConnected = ClientConnection.IsConnected;
            if (IsConnected)
            {
                ServerOs = GetServerOs();
                CheckForFilesAndCreateThemIfTheyNotExist();
            }
            if (ServerOs != PlatformID.Other) return IsConnected;
            exception = new NotSupportedException("No other OS than Windows oder Linux is supported!");
            return false;
        }
        catch (Exception e)
        {
            exception = e;
            return false;
        }
    }

    public bool CloseConnection([NotNullWhen(false)] out Exception? ex)
    {
        ex = null;
        try
        {
            ClientConnection.Disconnect();
            IsConnected = false;
            return true;
        }
        catch (Exception e)
        {
            ex = e;
            return false;
        }
    }

    public KnownHostsFile GetKnownHostsFromServer()
    {
        return !IsConnected
            ? new KnownHostsFile("", true)
            : new KnownHostsFile(
                ClientConnection
                    .RunCommand($"{ReadContentsCommand} {ResolveRemoteEnvVariables(SshConfigFiles.Known_Hosts.GetPathOfFile(false, ServerOs))}").Result,
                true);
    }

    public bool WriteKnownHostsToServer(KnownHostsFile knownHostsFile)
    {
        if (!IsConnected) return false;
        var command = ClientConnection.RunCommand(
            $"echo \"{knownHostsFile.GetUpdatedContents(ServerOs)}\" > {ResolveRemoteEnvVariables(SshConfigFiles.Known_Hosts.GetPathOfFile(false, ServerOs))}");
        return command.ExitStatus == 0;
    }

    public AuthorizedKeysFile GetAuthorizedKeysFromServer()
    {
        if (!IsConnected) return new AuthorizedKeysFile("", true);
        return new AuthorizedKeysFile(
            ClientConnection
                .RunCommand($"{ReadContentsCommand} {ResolveRemoteEnvVariables(SshConfigFiles.Authorized_Keys.GetPathOfFile(false, ServerOs))}")
                .Result, true);
    }

    public bool WriteAuthorizedKeysChangesToServer(AuthorizedKeysFile authorizedKeysFile)
    {
        if (!IsConnected) return false;
        return ClientConnection
            .RunCommand(
                $"echo \"{authorizedKeysFile.ExportFileContent(false, ServerOs)}\" > {ResolveRemoteEnvVariables(SshConfigFiles.Authorized_Keys.GetPathOfFile(false, ServerOs))}")
            .ExitStatus == 0;
    }
}