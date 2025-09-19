namespace OpenSSH_GUI.Core.Lib.Config;

/// <summary>
/// Represents a single 'Host' entry and its associated configuration values in an SSH config file.
/// </summary>
public class SshHostEntry
{
    /// <summary>
    /// Specifies the host alias or pattern for this entry.
    /// This is the value following the 'Host' keyword. It can be a single name or multiple names separated by spaces.
    /// </summary>
    public string Host { get; set; }

    /// <summary>
    /// The real hostname to log into.
    /// </summary>
    public string HostName { get; set; }

    /// <summary>
    /// The user to log in as.
    /// </summary>
    public string User { get; set; }

    /// <summary>
    /// The port to connect to on the remote host.
    /// Can be null if not specified, in which case the default SSH port (22) is used.
    /// </summary>
    public int? Port { get; set; }

    /// <summary>
    /// The path to the private key file for authentication.
    /// </summary>
    public string IdentityFile { get; set; }
    
    /// <summary>
    /// Specifies whether to forward the authentication agent connection.
    /// Can be null, "yes", or "no".
    /// </summary>
    public string ForwardAgent { get; set; }

    /// <summary>
    /// Specifies the number of seconds to wait for the connection to be established.
    /// Can be null if not specified.
    /// </summary>
    public int? ConnectTimeout { get; set; }

    /// <summary>
    /// Specifies whether to use strict host key checking.
    /// Can be null, "yes", "no", or "ask".
    /// </summary>
    public string StrictHostKeyChecking { get; set; }

    /// <summary>
    /// Specifies the path to the user's known hosts file.
    /// </summary>
    public string UserKnownHostsFile { get; set; }

    /// <summary>
    /// A dictionary to hold any other configuration key-value pairs that are not
    /// explicitly defined as properties in this class. This makes the parser flexible.
    /// The key is the SSH config keyword (e.g., "ServerAliveInterval"), and the value is its corresponding value string.
    /// </summary>
    public Dictionary<string, string> AdditionalProperties { get; } = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);
}