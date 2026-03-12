using OpenSSH_GUI.Core.Interfaces.Credentials;
using OpenSSH_GUI.Core.Lib.Credentials;
using OpenSSH_GUI.SshConfig;

namespace OpenSSH_GUI.Core.Extensions;

public static class SshConfigExtensions
{
    /// <summary>
    ///     Retrieves connection credentials from the provided SSH configuration document,
    ///     combining global and host-specific entries into a normalized set of connection entries.
    /// </summary>
    /// <param name="document">
    ///     The SSH configuration document that contains global entries and host blocks
    ///     from which the connection credentials will be extracted.
    /// </param>
    /// <returns>
    ///     An enumerable collection of <see cref="IConnectionCredentials" /> objects that
    ///     represent the normalized connection details, such as hostname, username, and authentication method.
    /// </returns>
    public static IEnumerable<IConnectionCredentials> GetConnectionEntriesFromConfig(this SshConfigDocument document)
    {
        var globalUser = document.GetGlobalEntries("User").FirstOrDefault()?.Value;
        var globalPort = document.GetGlobalEntries("Port").FirstOrDefault()?.Value;
        var globalIdentityFile = document.GetGlobalEntries("IdentityFile").FirstOrDefault()?.Value;

        foreach (var hostBlock in document.HostBlocks)
        foreach (var pattern in hostBlock.Patterns)
        {
            if (string.IsNullOrWhiteSpace(pattern) || pattern.Contains('*') || pattern.Contains('?') ||
                pattern.StartsWith('!'))
                continue;

            var hostName = hostBlock.GetEntry("HostName")?.Value ?? pattern;
            var user = hostBlock.GetEntry("User")?.Value ?? globalUser ?? Environment.UserName;
            var portStr = hostBlock.GetEntry("Port")?.Value ?? globalPort;
            var identityFile = hostBlock.GetEntry("IdentityFile")?.Value ?? globalIdentityFile;

            if (!string.IsNullOrEmpty(portStr) && portStr != "22" && !hostName.Contains(':'))
                hostName = $"{hostName}:{portStr}";

            if (!string.IsNullOrEmpty(identityFile))
                yield return new KeyConnectionCredentials(hostName, user, null);
            else
                yield return new PasswordConnectionCredentials(hostName, user, string.Empty);
        }
    }
}