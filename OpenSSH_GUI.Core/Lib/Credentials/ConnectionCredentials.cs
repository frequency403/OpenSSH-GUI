using System.Text.Json.Serialization;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Interfaces.Credentials;
using Renci.SshNet;

namespace OpenSSH_GUI.Core.Lib.Credentials;

/// <summary>
///     Represents the base class for connection credentials.
/// </summary>
public class ConnectionCredentials(string hostname, string username)
    : IConnectionCredentials
{
    /// <summary>
    ///     Represents the hostname of a server.
    ///     This property is used in classes related to connection credentials and server settings.
    /// </summary>
    public string Hostname { get; set; } = hostname;

    /// <summary>
    ///     Represents the port number used for establishing an SSH connection.
    /// </summary>
    public int Port => Hostname.Contains(':') ? int.Parse(Hostname.Split(':')[1]) : 22;

    /// <summary>
    ///     Represents the username property of a connection credentials.
    /// </summary>
    public string Username { get; set; } = username;

    /// <summary>
    ///     Retrieves the connection information based on the provided credentials.
    /// </summary>
    /// <returns>
    ///     The <see cref="ConnectionInfo" /> object representing the SSH connection information.
    /// </returns>
    public virtual ConnectionInfo GetConnectionInfo()
    {
        return new ConnectionInfo(Hostname, Username);
    }
}