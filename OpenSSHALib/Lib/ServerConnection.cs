using System.Diagnostics.CodeAnalysis;
using OpenSSHALib.Enums;
using OpenSSHALib.Extensions;
using OpenSSHALib.Models;
using ReactiveUI;
using Renci.SshNet;

namespace OpenSSHALib.Lib;

public class ServerConnection : ReactiveObject, IDisposable
{

    public ServerConnection(string hostname, string user, string password)
    {
        Hostname = hostname;
        Username = user;
        Password = password;
        _sshClient = new SshClient(Hostname, Username, Password)
        {
            KeepAliveInterval = TimeSpan.FromSeconds(10)
        };
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
    
    public string Hostname { get; }
    public string Username { get; } 
    public string Password { get; } 
    public SshPublicKey? AuthKey { get; set; }
    private SshClient _sshClient;
    private SshClient ClientConnection
    {
        get => _sshClient;
        set => this.RaiseAndSetIfChanged(ref _sshClient, value);
    }
    
    private bool _isConnected = false;
    public bool IsConnected
    {
        get => _isConnected;
        set => this.RaiseAndSetIfChanged(ref _isConnected, value);
    }
    
    public string ConnectionString => IsConnected ? $"{Username}@{Hostname}" : "";
    private PlatformID ServerOs { get; set; } = PlatformID.Other;
    private string ReadContentsCommand => ServerOs == PlatformID.Win32NT ? "type" : "cat";
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
            if(IsConnected) ServerOs = GetServerOs();
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

    public bool ReopenConnection()
    {
        if (IsConnected) return false;
        ClientConnection = new SshClient(Hostname, Username, Password);
        try
        {
            ClientConnection.Connect();
        }
        catch (Exception)
        {
            return false;
        }
        return IsConnected;
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
        return !IsConnected ? new KnownHostsFile("", true) : new KnownHostsFile(ClientConnection.RunCommand($"{ReadContentsCommand} {SshConfigFiles.Known_Hosts.GetPathOfFile(ServerOs)}").Result, true);
    }
    
    public bool WriteKnownHostsToServer(KnownHostsFile knownHostsFile)
    {
        if (!IsConnected) return false;
        var command = ClientConnection.RunCommand($"echo \"{knownHostsFile.GetUpdatedContents(ServerOs)}\" > {SshConfigFiles.Known_Hosts.GetPathOfFile(ServerOs)}");
        return command.ExitStatus == 0;
    }

    public AuthorizedKeysFile GetAuthorizedKeysFromServer()
    {
        if (!IsConnected) return new AuthorizedKeysFile("", true);
        return new AuthorizedKeysFile(
            ClientConnection.RunCommand($"{ReadContentsCommand} {SshConfigFiles.Authorized_Keys.GetPathOfFile(ServerOs)}")
                .Result, true);
    }

    public bool WriteAuthorizedKeysChangesToServer(AuthorizedKeysFile authorizedKeysFile)
    {
        if (!IsConnected) return false;
        return ClientConnection
            .RunCommand(
                $"echo \"{authorizedKeysFile.ExportFileContent(false, ServerOs)}\" > {SshConfigFiles.Authorized_Keys.GetPathOfFile(ServerOs)}")
            .ExitStatus == 0;
    }


    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
    void IDisposable.Dispose()
    {
        GC.SuppressFinalize(this);
    }
}