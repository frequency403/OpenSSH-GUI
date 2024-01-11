using System.Diagnostics.CodeAnalysis;
using OpenSSHALib.Enums;
using OpenSSHALib.Extensions;
using OpenSSHALib.Models;
using ReactiveUI;
using Renci.SshNet;

namespace OpenSSHALib.Lib;

public class ServerConnection(string hostname, string user, string password) : ReactiveObject, IDisposable
{
    private SshClient ClientConnection { get; }  = new (hostname, user, password);

    private bool _isConnected = false;
    public bool IsConnected
    {
        get => _isConnected;
        set => this.RaiseAndSetIfChanged(ref _isConnected, value);
    }
    
    public string ConnectionString => IsConnected ? $"{user}@{hostname}" : "";
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
    
    // public KnownHostsFile GetKnownHostsFromServer()
    // {
    //     return new KnownHostsFile(ClientConnection.RunCommand($"{ReadContentsCommand} {SshConfigFiles.Known_Hosts.GetPathOfFile(ServerOS)}").Result, true);
    // }
    //
    // public bool WriteKnownHostsToServer(KnownHostsFile knownHostsFile)
    // {
    //     var command = ClientConnection.RunCommand($"echo \"{knownHostsFile.GetUpdatedContents(ServerOS)}\" > {SshConfigFiles.Known_Hosts.GetPathOfFile(ServerOS)}");
    //     return command.ExitStatus == 0;
    // }

    public AuthorizedKeysFile GetAuthorizedKeysFromServer()
    {
        return new AuthorizedKeysFile(
            ClientConnection.RunCommand($"{ReadContentsCommand} {SshConfigFiles.Authorized_Keys.GetPathOfFile(ServerOs)}")
                .Result, true);
    }

    public bool WriteAuthorizedKeysChangesToServer(AuthorizedKeysFile authorizedKeysFile) => ClientConnection
            .RunCommand(
                $"echo \"{authorizedKeysFile.ExportFileContent(false, ServerOs)}\" > {SshConfigFiles.Authorized_Keys.GetPathOfFile(ServerOs)}")
            .ExitStatus == 0; 
    

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
    void IDisposable.Dispose()
    {
        GC.SuppressFinalize(this);
    }
}