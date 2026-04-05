using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Interfaces.Credentials;
using OpenSSH_GUI.Core.Lib.Misc;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace OpenSSH_GUI.Core.Services;

public partial class ServerConnectionService(ILogger<ServerConnectionService> logger) : ReactiveObject
{
    /// <summary>
    ///     Indicates whether the current server connection is active.
    /// </summary>
    /// <remarks>
    ///     This property returns <c>true</c> if a connection to the server exists
    ///     and is currently active. If the connection is not established or has
    ///     been terminated, it returns <c>false</c>.
    /// </remarks>
    [Reactive] 
    [property: MemberNotNullWhen(true, nameof(ServerConnection))]
    private bool _isConnected;

    /// <summary>
    ///     Gets or sets the server connection instance associated with the service.
    /// </summary>
    /// <remarks>
    ///     This property represents the current server connection being managed. It can be used
    ///     to retrieve or update the instance of the server connection. Setting this property
    ///     raises an internal change notification.
    /// </remarks>
    [Reactive]
    private ServerConnection? _serverConnection;

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
    /// <param name="throwOnNoConnection">Indicates whether to throw an exception if no connection exists.</param>
    /// <param name="token">
    ///     A <see cref="CancellationToken" /> used to cancel the operation if necessary.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask{TResult}" /> that represents the asynchronous operation.
    ///     The result is <see langword="true" /> if the connection was successfully closed; otherwise,
    ///     <see langword="false" />.
    ///     Throws <see cref="InvalidOperationException" /> if there is no active connection.
    /// </returns>
    public async ValueTask<bool> CloseConnection(bool throwOnNoConnection = true, CancellationToken token = default)
    {
        if (!IsConnected)
            return throwOnNoConnection ? throw new InvalidOperationException("No connection to disconnect from") : true;
        var disconnectResult = await ServerConnection.DisconnectFromServerAsync(token);
        if (disconnectResult)
            ServerConnection = null;
        return disconnectResult;
    }
}