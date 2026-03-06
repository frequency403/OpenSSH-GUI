using OpenSSH_GUI.Core.Interfaces.Credentials;
using OpenSSH_GUI.Core.Interfaces.Misc;
using ReactiveUI;

namespace OpenSSH_GUI.Core.Interfaces;

/// <summary>
/// Defines the operations and properties for managing server connections,
/// including establishing and closing connections.
/// </summary>
public interface IServerConnectionService : IReactiveNotifyPropertyChanged<IReactiveObject>, IHandleObservableErrors, IReactiveObject
{
    /// <summary>
    /// Indicates whether the current server connection is active.
    /// </summary>
    /// <remarks>
    /// This property returns <c>true</c> if a connection to the server exists
    /// and is currently active. If the connection is not established or has
    /// been terminated, it returns <c>false</c>.
    /// </remarks>
    bool IsConnected { get; }

    /// <summary>
    /// Gets or sets the server connection instance associated with the service.
    /// </summary>
    /// <remarks>
    /// This property represents the current server connection being managed. It can be used
    /// to retrieve or update the instance of the server connection. Setting this property
    /// raises an internal change notification.
    /// </remarks>
    IServerConnection? ServerConnection { get; set; }

    /// <summary>
    /// Establishes a connection to a server using the provided connection credentials.
    /// </summary>
    /// <param name="connectionCredentials">
    /// The connection credentials containing the necessary information to connect to the server (e.g., hostname, port, username, and authentication type).
    /// </param>
    /// <param name="token">
    /// A cancellation token that can be used to propagate a cancellation request or observe cancellations of the asynchronous operation.
    /// </param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> representing the result of the connection attempt.
    /// Returns <c>true</c> if the connection is successfully established; otherwise, <c>false</c>.
    /// </returns>
    ValueTask<bool> EstablishConnection(IConnectionCredentials connectionCredentials, CancellationToken token = default);

    /// <summary>
    /// Closes the current connection to the server, if a connection exists.
    /// </summary>
    /// <param name="token">
    /// A <see cref="CancellationToken"/> used to cancel the operation if necessary.
    /// </param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> that represents the asynchronous operation.
    /// The result is <see langword="true"/> if the connection was successfully closed; otherwise, <see langword="false"/>.
    /// Throws <see cref="InvalidOperationException"/> if there is no active connection.
    /// </returns>
    ValueTask<bool> CloseConnection(CancellationToken token = default);
}