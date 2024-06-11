#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:33

#endregion

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
    ///     Tests the connection to the server and opens a connection if successful.
    /// </summary>
    /// <param name="exception">
    ///     An <see cref="Exception" /> that represents the error that occurred during the test and open
    ///     connection process, if any. If the test and open connection process is successful, this value is <c>null</c>.
    /// </param>
    /// <returns><c>true</c> if the test and open connection process is successful; otherwise, <c>false</c>.</returns>
    bool TestAndOpenConnection([NotNullWhen(false)] out Exception? exception);

    /// <summary>
    ///     Closes the server connection.
    /// </summary>
    /// <param name="ex">An exception that occurred during the closing of the connection, if any.</param>
    /// <returns>Returns true if the connection was closed successfully, otherwise false.</returns>
    bool CloseConnection([NotNullWhen(false)] out Exception? ex);

    /// <summary>
    ///     Retrieves the known hosts file from the server.
    /// </summary>
    /// <returns>The known hosts file as an instance of IKnownHostsFile.</returns>
    IKnownHostsFile GetKnownHostsFromServer();

    /// <summary>
    ///     Writes the known hosts file to the server.
    /// </summary>
    /// <param name="knownHostsFile">The known hosts file to write.</param>
    /// <returns>Returns true if the known hosts file was successfully written to the server, false otherwise.</returns>
    bool WriteKnownHostsToServer(IKnownHostsFile knownHostsFile);

    /// <summary>
    ///     Retrieves the authorized keys file from the server.
    /// </summary>
    /// <returns>The authorized keys file from the server.</returns>
    IAuthorizedKeysFile GetAuthorizedKeysFromServer();

    /// <summary>
    ///     Writes the changes made to the authorized keys file to the server.
    /// </summary>
    /// <param name="authorizedKeysFile">The authorized keys file containing the changes.</param>
    /// <returns>True if the changes were successfully written to the server; otherwise, false.</returns>
    bool WriteAuthorizedKeysChangesToServer(IAuthorizedKeysFile authorizedKeysFile);
}