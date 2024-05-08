﻿#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 08.05.2024 - 22:05:30
// Last edit: 08.05.2024 - 22:05:56

#endregion

using System.Diagnostics.CodeAnalysis;
using OpenSSHALib.Enums;
using OpenSSHALib.Extensions;
using OpenSSHALib.Interfaces;
using OpenSSHALib.Models;
using ReactiveUI;
using Renci.SshNet;

namespace OpenSSHALib.Lib;

public class ServerConnection : ReactiveObject, IServerConnection
{
    private readonly IConnectionCredentials ConnectionCredentials;
    private DateTime _connectionTime = DateTime.Now;
    private bool _isConnected;
    private SshClient _sshClient;

    public ServerConnection(IConnectionCredentials credentials)
    {
        ConnectionCredentials = credentials;
        _sshClient = new SshClient(credentials.GetConnectionInfo()) { KeepAliveInterval = TimeSpan.FromSeconds(10) };
        ConnectionTime = DateTime.Now;
    }

    public ServerConnection(string hostname, string user, string password) : this(new PasswordConnectionCredentials
    {
        Hostname = hostname,
        Username = user,
        Password = password
    })
    {
    }

    public ServerConnection(string hostname, string user, ISshKey key) : this(new KeyConnectionCredentials
    {
        Hostname = hostname,
        Username = user,
        PublicKey = key
    })
    {
    }

    private SshClient ClientConnection
    {
        get => _sshClient;
        set => this.RaiseAndSetIfChanged(ref _sshClient, value);
    }

    private string ReadContentsCommand => ServerOs == PlatformID.Win32NT ? "type" : "cat";
    private string CreateEmptyFileCommand => ServerOs == PlatformID.Win32NT ? "echo. >" : "touch";

    public DateTime ConnectionTime
    {
        get => _connectionTime;
        set => this.RaiseAndSetIfChanged(ref _connectionTime, value);
    }

    public bool IsConnected
    {
        get => _isConnected;
        set => this.RaiseAndSetIfChanged(ref _isConnected, value);
    }

    public string ConnectionString =>
        IsConnected ? $"{ConnectionCredentials.Username}@{ConnectionCredentials.Hostname}" : "";

    public PlatformID ServerOs { get; set; } = PlatformID.Other;

    /// <summary>Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.</summary>
    void IDisposable.Dispose()
    {
        GC.SuppressFinalize(this);
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
                ConnectionTime = DateTime.Now;
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

    public IKnownHostsFile GetKnownHostsFromServer()
    {
        return !IsConnected
            ? new KnownHostsFile("", true)
            : new KnownHostsFile(
                ClientConnection
                    .RunCommand(
                        $"{ReadContentsCommand} {ResolveRemoteEnvVariables(SshConfigFiles.Known_Hosts.GetPathOfFile(false, ServerOs))}")
                    .Result,
                true);
    }

    public bool WriteKnownHostsToServer(IKnownHostsFile knownHostsFile)
    {
        if (!IsConnected) return false;
        var command = ClientConnection.RunCommand(
            $"echo \"{knownHostsFile.GetUpdatedContents(ServerOs)}\" > {ResolveRemoteEnvVariables(SshConfigFiles.Known_Hosts.GetPathOfFile(false, ServerOs))}");
        return command.ExitStatus == 0;
    }

    public IAuthorizedKeysFile GetAuthorizedKeysFromServer()
    {
        if (!IsConnected) return new AuthorizedKeysFile("", true);
        return new AuthorizedKeysFile(
            ClientConnection
                .RunCommand(
                    $"{ReadContentsCommand} {ResolveRemoteEnvVariables(SshConfigFiles.Authorized_Keys.GetPathOfFile(false, ServerOs))}")
                .Result, true);
    }

    public bool WriteAuthorizedKeysChangesToServer(IAuthorizedKeysFile authorizedKeysFile)
    {
        if (!IsConnected) return false;
        return ClientConnection
            .RunCommand(
                $"echo \"{authorizedKeysFile.ExportFileContent(false, ServerOs)}\" > {ResolveRemoteEnvVariables(SshConfigFiles.Authorized_Keys.GetPathOfFile(false, ServerOs))}")
            .ExitStatus == 0;
    }

    private string ResolveRemoteEnvVariables(string originalPath)
    {
        if (!IsConnected) return originalPath;
        return originalPath.Split('%', StringSplitOptions.RemoveEmptyEntries).Aggregate("", (s, s1) =>
        {
            if (s1.Contains('\\') || s1.Contains('/'))
                s += s1.Trim();
            else
                s += ClientConnection
                    .RunCommand(ServerOs is PlatformID.Unix or PlatformID.MacOSX ? $"echo ${s1}" : $"echo %{s1}%")
                    .Result.Trim();
            return s;
        });
    }

    private void CheckForFilesAndCreateThemIfTheyNotExist()
    {
        if (!ClientConnection.IsConnected) return;
        var authorizedKeysFileCheck =
            ClientConnection.RunCommand($"{ReadContentsCommand} {SshConfigFiles.Authorized_Keys.GetPathOfFile(false)}");
        var knownHostsFileCheck =
            ClientConnection.RunCommand($"{ReadContentsCommand} {SshConfigFiles.Known_Hosts.GetPathOfFile(false)}");
        if (authorizedKeysFileCheck.ExitStatus != 0)
            ClientConnection.RunCommand(
                $"{CreateEmptyFileCommand} {SshConfigFiles.Authorized_Keys.GetPathOfFile(false)}");
        if (knownHostsFileCheck.ExitStatus != 0)
            ClientConnection.RunCommand($"{CreateEmptyFileCommand} {SshConfigFiles.Known_Hosts.GetPathOfFile(false)}");
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
}