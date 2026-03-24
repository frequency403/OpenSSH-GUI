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
    ///     Retrieves the connection information based on the provided credentials.
    /// </summary>
    /// <returns>The <see cref="ConnectionInfo" /> object representing the SSH connection information.</returns>
    ConnectionInfo GetConnectionInfo();
}