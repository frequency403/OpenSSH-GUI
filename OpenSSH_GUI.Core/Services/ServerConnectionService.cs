using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Interfaces.Credentials;
using OpenSSH_GUI.Core.Interfaces.Misc;
using OpenSSH_GUI.Core.Lib.Misc;
using ReactiveUI;

namespace OpenSSH_GUI.Core.Services;

public class ServerConnectionService(ILogger<ServerConnectionService> logger) : ReactiveObject
{
    /// <summary>
    ///     Indicates whether the current server connection is active.
    /// </summary>
    /// <remarks>
    ///     This property returns <c>true</c> if a connection to the server exists
    ///     and is currently active. If the connection is not established or has
    ///     been terminated, it returns <c>false</c>.
    /// </remarks>
    [MemberNotNullWhen(true, nameof(ServerConnection))]
    public bool IsConnected
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    /// <summary>
    ///     Gets or sets the server connection instance associated with the service.
    /// </summary>
    /// <remarks>
    ///     This property represents the current server connection being managed. It can be used
    ///     to retrieve or update the instance of the server connection. Setting this property
    ///     raises an internal change notification.
    /// </remarks>
    public IServerConnection? ServerConnection
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            IsConnected = value != null;
        }
    }

    /// <summary>
    ///     Establishes a connection to a server using the provided connection credentials.
    /// </summary>
    /// <param name="connectionCredentials">
    ///     The connection credentials containing the necessary information to connect to the server (e.g., hostname, port,
    ///     username, and authentication type).
    /// </param>
    /// <param name="token">
    ///     A cancellation token that can be used to propagate a cancellation request or observe cancellations of the
    ///     asynchronous operation.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask{TResult}" /> representing the result of the connection attempt.
    ///     Returns <c>true</c> if the connection is successfully established; otherwise, <c>false</c>.
    /// </returns>
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

    /// <summary>
    ///     Closes the current connection to the server, if a connection exists.
    /// </summary>
    /// <param name="token">
    ///     A <see cref="CancellationToken" /> used to cancel the operation if necessary.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask{TResult}" /> that represents the asynchronous operation.
    ///     The result is <see langword="true" /> if the connection was successfully closed; otherwise,
    ///     <see langword="false" />.
    ///     Throws <see cref="InvalidOperationException" /> if there is no active connection.
    /// </returns>
    public async ValueTask<bool> CloseConnection(CancellationToken token = default)
    {
        if (!IsConnected) throw new InvalidOperationException("No connection to disconnect from");
        var disconnectResult = await ServerConnection.DisconnectFromServerAsync(token);
        if (disconnectResult)
            ServerConnection = null;
        return disconnectResult;
    }
}