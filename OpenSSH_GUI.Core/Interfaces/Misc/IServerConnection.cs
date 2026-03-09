using System.Diagnostics.CodeAnalysis;
using OpenSSH_GUI.Core.Interfaces.AuthorizedKeys;
using OpenSSH_GUI.Core.Interfaces.Credentials;
using OpenSSH_GUI.Core.Interfaces.KnownHosts;
using ReactiveUI;

namespace OpenSSH_GUI.Core.Interfaces.Misc;

/// Represents a server connection.
/// /
public interface IServerConnection : IReactiveObject, IDisposable
{
    /// <summary>
    ///     Represents the credentials for a server connection.
    /// </summary>
    IConnectionCredentials ConnectionCredentials { get; }

    /// <summary>
    ///     Gets or sets the connection time of the server.
    /// </summary>
    DateTime ConnectionTime { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the server connection is currently open.
    /// </summary>
    bool IsConnected { get; set; }

    /// <summary>
    ///     Represents a server connection.
    /// </summary>
    string ConnectionString { get; }

    /// <summary>
    ///     Represents a server connection.
    /// </summary>
    PlatformID ServerOs { get; set; }

    /// <summary>
    ///     Establishes a connection to the server using the provided credentials and updates the connection state.
    /// </summary>
    /// <param name="token">
    ///     A <see cref="CancellationToken" /> that can be used to cancel the connection attempt.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask{Boolean}" /> representing the asynchronous connection operation.
    ///     Returns <c>true</c> if the connection is established successfully; otherwise, <c>false</c>.
    /// </returns>
    ValueTask<bool> ConnectToServerAsync(CancellationToken token = default);

    /// <summary>
    ///     Disconnects from the server and updates the connection state asynchronously.
    /// </summary>
    /// <param name="token">
    ///     A <see cref="CancellationToken" /> that can be used to cancel the disconnection attempt.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask{Boolean}" /> representing the asynchronous disconnection operation.
    ///     Returns <c>true</c> if the disconnection is successful; otherwise, <c>false</c>.
    /// </returns>
    ValueTask<bool> DisconnectFromServerAsync(CancellationToken token = default);

    /// <summary>
    ///     Closes the server connection asynchronously.
    /// </summary>
    /// <param name="token">A <see cref="CancellationToken" /> that can be used to cancel the operation.</param>
    /// <returns>A <see cref="ValueTask{Boolean}"/> indicating whether the connection was closed successfully.</returns>
    ValueTask<bool> CloseConnectionAsync(CancellationToken token = default);

    /// <summary>
    ///     Retrieves the known hosts file from the server asynchronously.
    /// </summary>
    /// <param name="token">A <see cref="CancellationToken" /> that can be used to cancel the operation.</param>
    /// <returns>A <see cref="ValueTask{IKnownHostsFile}"/> representing the known hosts file.</returns>
    ValueTask<IKnownHostsFile> GetKnownHostsFromServerAsync(CancellationToken token = default);

    /// <summary>
    ///     Writes the known hosts file to the server asynchronously.
    /// </summary>
    /// <param name="knownHostsFile">The known hosts file to write.</param>
    /// <param name="token">A <see cref="CancellationToken" /> that can be used to cancel the operation.</param>
    /// <returns>A <see cref="ValueTask{Boolean}"/> indicating whether the known hosts file was successfully written.</returns>
    ValueTask<bool> WriteKnownHostsToServerAsync(IKnownHostsFile knownHostsFile, CancellationToken token = default);

    /// <summary>
    ///     Retrieves the authorized keys file from the server asynchronously.
    /// </summary>
    /// <param name="token">A <see cref="CancellationToken" /> that can be used to cancel the operation.</param>
    /// <returns>A <see cref="ValueTask{IAuthorizedKeysFile}"/> representing the authorized keys file.</returns>
    ValueTask<IAuthorizedKeysFile> GetAuthorizedKeysFromServerAsync(CancellationToken token = default);

    /// <summary>
    ///     Writes the changes made to the authorized keys file to the server asynchronously.
    /// </summary>
    /// <param name="authorizedKeysFile">The authorized keys file containing the changes.</param>
    /// <param name="token">A <see cref="CancellationToken" /> that can be used to cancel the operation.</param>
    /// <returns>A <see cref="ValueTask{Boolean}"/> indicating whether the changes were successfully written.</returns>
    ValueTask<bool> WriteAuthorizedKeysChangesToServerAsync(IAuthorizedKeysFile authorizedKeysFile, CancellationToken token = default);
}