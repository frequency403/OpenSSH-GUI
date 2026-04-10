using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Lib.Misc;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace OpenSSH_GUI.Core.Services;

public sealed partial class ServerConnectionService : ReactiveObject, IDisposable
{
    private readonly CompositeDisposable _disposables = new();

    private readonly ILogger<ServerConnectionService> _logger;

    /// <summary>
    ///     Indicates whether the current server connection is active.
    /// </summary>
    /// <remarks>
    ///     This property returns <c>true</c> if a connection to the server exists
    ///     and is currently active. If the connection is not established or has
    ///     been terminated, it returns <c>false</c>.
    /// </remarks>
    [ObservableAsProperty(ReadOnly = true)]
    private bool _isConnected;

    /// <summary>
    ///     Gets or sets the server connection instance associated with the service.
    /// </summary>
    /// <remarks>
    ///     This property represents the current server connection being managed. It can be used
    ///     to retrieve or update the instance of the server connection. Setting this property
    ///     raises an internal change notification.
    /// </remarks>
    [Reactive(SetModifier = AccessModifier.Private)]
    private ServerConnection _serverConnection = ServerConnection.Empty;

    public ServerConnectionService(ILogger<ServerConnectionService> logger)
    {
        _logger = logger;

        _isConnectedHelper = this.WhenAnyValue(vm => vm.ServerConnection)
            .Select(e => e.WhenAnyValue(sc => sc.IsConnected))
            .Switch()
            .ToProperty(this, obj => obj.IsConnected);
    }

    public void Dispose()
    {
        _disposables.Dispose();
        _serverConnection.Dispose();
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
    public async ValueTask<bool> EstablishConnection(ConnectionCredentials connectionCredentials,
        CancellationToken token = default)
    {
        try
        {
            ServerConnection = ServerConnection.WithCredentials(connectionCredentials);
            return await ServerConnection.ConnectToServerAsync(token);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error connecting to server");
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
        ServerConnection.Dispose();
        if (disconnectResult)
            ServerConnection = ServerConnection.Empty;
        return disconnectResult;
    }
}