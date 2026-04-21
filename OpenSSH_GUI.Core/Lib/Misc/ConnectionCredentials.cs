using OpenSSH_GUI.Core.Lib.Keys;
using Renci.SshNet;

namespace OpenSSH_GUI.Core.Lib.Misc;

/// <summary>
///     Represents the base class for connection credentials.
/// </summary>
public abstract class ConnectionCredentials
{
    private const string Placeholder = "123";

    /// <summary>
    ///     Represents the base class for connection credentials.
    /// </summary>
    protected ConnectionCredentials(string hostname, string username)
    {
        if (hostname.Contains(':'))
        {
            var split = hostname.Split(':');
            Hostname = split[0];
            Port = int.Parse(split[1]);
        }
        else
        {
            Hostname = hostname;
            Port = 22;
        }

        Username = username;
    }

    internal static ConnectionCredentials Empty { get; } =
        new PasswordConnectionCredentials(Placeholder, Placeholder, Placeholder);

    /// <summary>
    ///     Represents the hostname of a server.
    ///     This property is used in classes related to connection credentials and server settings.
    /// </summary>
    public string Hostname { get; }

    /// <summary>
    ///     Represents the port number used for establishing an SSH connection.
    /// </summary>
    public int Port { get; }

    /// <summary>
    ///     Represents the username property of a connection credentials.
    /// </summary>
    public string Username { get; }

    /// <summary>
    ///     Retrieves the connection information based on the provided credentials.
    /// </summary>
    /// <returns>
    ///     The <see cref="ConnectionInfo" /> object representing the SSH connection information.
    /// </returns>
    public virtual ConnectionInfo GetConnectionInfo()
    {
        return AddAuthenticationMethods(new NoneAuthenticationMethod(Username));
    }

    protected ConnectionInfo AddAuthenticationMethods(params AuthenticationMethod[] methods)
    {
        return new ConnectionInfo(Hostname, Port, Username, methods);
    }
}

/// <summary>
///     Represents the credentials for a key-based connection to a server.
/// </summary>
public class KeyConnectionCredentials(string hostname, string username, SshKeyFile? key)
    : ConnectionCredentials(hostname, username)
{
    /// <summary>
    ///     Retrieves the connection information based on the provided credentials.
    /// </summary>
    /// <returns>
    ///     The <see cref="ConnectionInfo" /> object representing the SSH connection information.
    /// </returns>
    public override ConnectionInfo GetConnectionInfo()
    {
        return AddAuthenticationMethods(new PrivateKeyAuthenticationMethod(Username, key?.PrivateKeyFile));
    }
}

public class MultiKeyConnectionCredentials(string hostname, string username, IEnumerable<SshKeyFile>? keys)
    : ConnectionCredentials(hostname, username)
{
    /// <summary>
    ///     Retrieves the connection information for establishing an SSH connection.
    /// </summary>
    /// <returns>
    ///     The <see cref="ConnectionInfo" /> object representing the SSH connection information.
    /// </returns>
    public override ConnectionInfo GetConnectionInfo()
    {
        return keys is null
            ? base.GetConnectionInfo()
            : AddAuthenticationMethods(keys.Select(e => new PrivateKeyAuthenticationMethod(Username, e.PrivateKeyFile)
            ).ToArray<AuthenticationMethod>());
    }
}

public class PasswordConnectionCredentials(
    string hostname,
    string username,
    string password)
    : ConnectionCredentials(hostname, username)
{
    /// <summary>
    ///     Retrieves the connection information based on the provided credentials.
    /// </summary>
    /// <returns>The <see cref="ConnectionInfo" /> object representing the SSH connection information.</returns>
    public override ConnectionInfo GetConnectionInfo()
    {
        return AddAuthenticationMethods(new PasswordAuthenticationMethod(Username, password));
    }
}