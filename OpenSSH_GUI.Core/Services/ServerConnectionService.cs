using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Interfaces.Credentials;
using OpenSSH_GUI.Core.Interfaces.Misc;
using OpenSSH_GUI.Core.Interfaces.Services;
using OpenSSH_GUI.Core.Lib.Misc;
using ReactiveUI;

namespace OpenSSH_GUI.Core.Services;

public class ServerConnectionService(ILogger<ServerConnectionService> logger) : ReactiveObject, IServerConnectionService
{
    [MemberNotNullWhen(true, nameof(ServerConnection))]
    public bool IsConnected => ServerConnection?.IsConnected ?? false;

    public IServerConnection? ServerConnection
    {
        get => field;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public async ValueTask<bool> EstablishConnection(IConnectionCredentials connectionCredentials,
        CancellationToken token = default)
    {
        try
        {
            ServerConnection = new ServerConnection(connectionCredentials);
            return await ServerConnection.ConnectToServerAsync(token);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error connecting to server");
            throw;
        }
    }

    public async ValueTask<bool> CloseConnection(CancellationToken token = default)
    {
        if (!IsConnected) throw new InvalidOperationException("No connection to disconnect from");
        var disconnectResult = await ServerConnection.DisconnectFromServerAsync(token);
        if (disconnectResult)
            ServerConnection = null;
        return disconnectResult;
    }
}