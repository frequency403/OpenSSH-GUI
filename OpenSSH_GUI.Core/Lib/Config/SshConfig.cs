namespace OpenSSH_GUI.Core.Lib.Config;

/// <summary>
/// Represents a complete SSH configuration, which consists of multiple host entries.
/// </summary>
public class SshConfig
{
    /// <summary>
    /// Gets or sets the list of all host entries found in the configuration file.
    /// The order of entries is preserved.
    /// </summary>
    public List<SshHostEntry> HostEntries { get; set; } = new List<SshHostEntry>();

    /// <summary>
    /// Finds the first host entry that matches the specified host alias.
    /// SSH configuration allows for wildcard matching, but this method performs a simple exact match.
    /// </summary>
    /// <param name="hostAlias">The host alias to search for (e.g., "my-server").</param>
    /// <returns>The first matching SshHostEntry or null if no entry is found.</returns>
    public SshHostEntry? FindHostEntry(string hostAlias)
    {
        return HostEntries.FirstOrDefault(entry => 
            entry.Host.Split(' ').Contains(hostAlias, System.StringComparer.OrdinalIgnoreCase));
    }
}