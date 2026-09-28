using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Interfaces;
using OpenSSH_GUI.Core.Lib.HostKeys;
using OpenSSH_GUI.Core.Lib.Misc;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace OpenSSH_GUI.Core.Services;

public sealed partial class ServerConnectionService : ReactiveObject, IDisposable
{
    private readonly CompositeDisposable _disposables = new();

    private readonly IHostKeyTrustPrompt _hostKeyTrustPrompt;

    private readonly IKnownHostKeyStore _knownHostKeyStore;

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

    public ServerConnectionService(ILogger<ServerConnectionService> logger, IKnownHostKeyStore knownHostKeyStore,
        IHostKeyTrustPrompt hostKeyTrustPrompt)
    {
        _logger = logger;
        _knownHostKeyStore = knownHostKeyStore;
        _hostKeyTrustPrompt = hostKeyTrustPrompt;

        _isConnectedHelper = this.WhenAnyValue(vm => vm.ServerConnection)
            .Select(e => e.WhenAnyValue(sc => sc.IsConnected))
            .Switch()
            .ToProperty(this, obj => obj.IsConnected)
            .DisposeWith(_disposables);
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
            var (connected, rejectedKey) = await TryConnectAsync(connectionCredentials, token);
            if (rejectedKey is null) return connected;

            if (rejectedKey.Status is not HostKeyVerificationStatus.Unknown)
                throw new HostKeyVerificationException(rejectedKey.HostKey, rejectedKey.Status);

            // Trust on first use: the user has to confirm the fingerprint before the key is stored.
            if (!await _hostKeyTrustPrompt.ConfirmUnknownHostKeyAsync(rejectedKey.HostKey))
                throw new HostKeyVerificationException(rejectedKey.HostKey, rejectedKey.Status);

            _knownHostKeyStore.Add(rejectedKey.HostKey);
            (connected, rejectedKey) = await TryConnectAsync(connectionCredentials, token);
            return rejectedKey is null
                ? connected
                : throw new HostKeyVerificationException(rejectedKey.HostKey, rejectedKey.Status);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error connecting to server");
            throw;
        }
    }

    /// <summary>
    ///     Connects with host key verification against <c>known_hosts</c>.
    /// </summary>
    /// <returns>
    ///     The connection result, or the rejected host key if the connection was aborted
    ///     because the host key could not be verified.
    /// </returns>
    private async ValueTask<(bool Connected, RejectedHostKey? RejectedKey)> TryConnectAsync(
        ConnectionCredentials connectionCredentials, CancellationToken token)
    {
        RejectedHostKey? rejectedKey = null;
        DisposeCurrentConnection();
        ServerConnection = ServerConnection.WithCredentials(
            connectionCredentials, hostKey =>
            {
                var status = _knownHostKeyStore.Verify(hostKey);
                if (status is HostKeyVerificationStatus.Trusted) return true;
                rejectedKey ??= new RejectedHostKey(hostKey, status);
                return false;
            });

        try
        {
            return (await ServerConnection.ConnectToServerAsync(token), null);
        }
        catch (Exception e) when (rejectedKey is not null)
        {
            _logger.LogWarning(
                e, "Host key {fingerprint} of {host} rejected: {status}", rejectedKey.HostKey.FingerprintSha256,
                rejectedKey.HostKey.KnownHostsName, rejectedKey.Status);
            DisposeCurrentConnection();
            ServerConnection = ServerConnection.Empty;
            return (false, rejectedKey);
        }
    }

    private void DisposeCurrentConnection()
    {
        if (!ReferenceEquals(ServerConnection, ServerConnection.Empty))
            ServerConnection.Dispose();
    }

    private sealed record RejectedHostKey(HostKeyInfo HostKey, HostKeyVerificationStatus Status);

    /// <summary>
    ///     Closes the current connection to the server if a connection exists.
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