#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:34

#endregion

using System.Text.Json.Serialization;
using OpenSSH_GUI.Core.Enums;
using Renci.SshNet;

namespace OpenSSH_GUI.Core.Interfaces.Credentials;

/// <summary>
///     Represents the interface for connection credentials.
/// </summary>
public interface IConnectionCredentials
{
    /// <summary>
    ///     Represents the unique identifier for a connection credential.
    /// </summary>
    /// <remarks>
    ///     The ID is used to uniquely identify a connection credential in the system.
    /// </remarks>
    int Id { get; set; }

    /// <summary>
    ///     Represents the host name for a connection.
    /// </summary>
    /// <remarks>
    ///     The host name is an essential property for establishing a connection to a remote server.
    ///     It identifies the target server that the client wants to connect to.
    /// </remarks>
    string Hostname { get; set; }

    /// <summary>
    ///     Represents the base class for connection credentials.
    /// </summary>
    int Port { get; }

    /// <summary>
    ///     Represents the username used for the connection credentials.
    /// </summary>
    string Username { get; set; }

    /// <summary>
    ///     Gets the display string representation of the connection credentials.
    /// </summary>
    /// <value>The display string in the format "{Username}@{Hostname}:{Port}"</value>
    [JsonIgnore]
    string Display { get; }

    /// <summary>
    ///     Gets the authentication type for the connection credentials.
    /// </summary>
    /// <remarks>
    ///     The <see cref="AuthType" /> property represents the type of authentication to be used for the connection
    ///     credentials.
    ///     The available authentication types are:
    ///     - Password: The connection credentials use password authentication.
    ///     - Key: The connection credentials use key authentication.
    ///     - MultiKey: The connection credentials use multi-key authentication.
    /// </remarks>
    AuthType AuthType { get; }

    /// <summary>
    ///     Retrieves the connection information based on the provided credentials.
    /// </summary>
    /// <returns>The <see cref="ConnectionInfo" /> object representing the SSH connection information.</returns>
    ConnectionInfo GetConnectionInfo();
}