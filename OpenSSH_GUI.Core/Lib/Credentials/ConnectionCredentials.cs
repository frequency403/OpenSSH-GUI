#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:31

#endregion

using System.Text.Json.Serialization;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Interfaces.Credentials;
using Renci.SshNet;

namespace OpenSSH_GUI.Core.Lib.Credentials;

/// <summary>
/// Represents the base class for connection credentials.
/// </summary>
public abstract class ConnectionCredentials(string hostname, string username, AuthType authType)
    : IConnectionCredentials
{
    /// <summary>
    /// Represents the hostname of a server.
    /// This property is used in classes related to connection credentials and server settings.
    /// </summary>
    public string Hostname { get; set; } = hostname;

    /// <summary>
    /// Represents the port number used for establishing an SSH connection.
    /// </summary>
    public int Port => Hostname.Contains(':') ? int.Parse(Hostname.Split(':')[1]) : 22;

    /// <summary>
    /// Represents the username property of a connection credentials.
    /// </summary>
    public string Username { get; set; } = username;

    /// <summary>
    /// Retrieves the connection information based on the provided credentials.
    /// </summary>
    /// <returns>
    /// The <see cref="ConnectionInfo"/> object representing the SSH connection information.
    /// </returns>
    public abstract ConnectionInfo GetConnectionInfo();

    /// <summary>
    /// Gets the display string for the connection credentials.
    /// </summary>
    [JsonIgnore] public string Display => ToString();

    /// <summary>
    /// Represents the authentication type for a connection.
    /// </summary>
    public AuthType AuthType { get; } = authType;

    /// <summary>
    /// Returns a string representation of the ConnectionCredentials object.
    /// </summary>
    /// <returns>A string in the format "{Username}@{Hostname}:{Port}"</returns>
    public override string ToString()
    {
        return $"{Username}@{Hostname}{(Port is 22 ? "" : $":{Port}")}";
    }
}