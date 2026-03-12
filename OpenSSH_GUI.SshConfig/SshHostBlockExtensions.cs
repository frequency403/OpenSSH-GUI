using System.Collections.Immutable;

namespace OpenSSH_GUI.SshConfig;

/// <summary>
///     Extension methods for <see cref="SshHostBlock" /> to convert to and from <see cref="SshHostSettings" />.
/// </summary>
public static class SshHostBlockExtensions
{
    /// <summary>
    ///     Converts an <see cref="SshBlock" /> to a type-safe <see cref="SshHostSettings" /> record.
    /// </summary>
    /// <param name="block">The block to convert.</param>
    /// <returns>A type-safe <see cref="SshHostSettings" /> representation of the block.</returns>
    public static SshHostSettings GetSettings(this SshBlock block)
    {
        return GetSettingsFromEntries(block.GetEntries(),
            block is SshHostBlock hostBlock ? hostBlock.Patterns.ToArray() : null);
    }

    /// <summary>
    ///     Extracts <see cref="SshHostSettings" /> from a collection of <see cref="SshConfigEntry" />.
    /// </summary>
    /// <param name="entries">The entries to extract settings from.</param>
    /// <param name="patterns">Optional patterns (e.g. from a Host block header).</param>
    /// <returns>A type-safe <see cref="SshHostSettings" /> representation.</returns>
    public static SshHostSettings GetSettingsFromEntries(IEnumerable<SshConfigEntry> entries, string[]? patterns = null)
    {
        string? hostName = null;
        string? user = null;
        int? port = null;
        string? proxyJump = null;
        var identityFiles = new List<string>();
        var localForwards = new List<string>();
        var otherEntries = new List<SshConfigEntry>();

        foreach (var entry in entries)
            switch (entry.Key.ToLowerInvariant())
            {
                case "hostname":
                    hostName ??= entry.Value;
                    break;
                case "user":
                    user ??= entry.Value;
                    break;
                case "port":
                    if (port == null && int.TryParse(entry.Value, out var p))
                        port = p;
                    else
                        otherEntries.Add(entry);
                    break;
                case "identityfile":
                    if (entry.Value != null)
                        identityFiles.Add(entry.Value);
                    break;
                case "proxyjump":
                    proxyJump ??= entry.Value;
                    break;
                case "localforward":
                    if (entry.Value != null)
                        localForwards.Add(string.Join(' ', entry.Values));
                    break;
                default:
                    otherEntries.Add(entry);
                    break;
            }

        return new SshHostSettings(
            patterns ?? Array.Empty<string>(),
            hostName,
            user,
            port,
            identityFiles.ToArray(),
            proxyJump,
            localForwards.ToArray(),
            otherEntries.ToArray()
        );
    }

    /// <summary>
    ///     Creates a new <see cref="SshHostBlock" /> by applying the provided <paramref name="settings" />
    ///     to the existing block, replacing any existing mapped entries.
    /// </summary>
    /// <param name="block">The original block.</param>
    /// <param name="settings">The new settings to apply.</param>
    /// <returns>A new <see cref="SshHostBlock" /> with the updated settings.</returns>
    public static SshHostBlock WithSettings(this SshHostBlock block, SshHostSettings settings)
    {
        var newItems = ImmutableArray.CreateBuilder<SshLineItem>();

        // We want to preserve the order and non-entry items (comments, blanks)
        // But we also want to replace existing entries that are now in settings.

        var handledKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "HostName", "User", "Port", "IdentityFile", "ProxyJump", "LocalForward"
        };

        var addedHostName = settings.HostName == null;
        var addedUser = settings.User == null;
        var addedPort = settings.Port == null;
        var addedProxyJump = settings.ProxyJump == null;
        var addedIdentityFiles = settings.IdentityFiles == null || settings.IdentityFiles.Length == 0;
        var addedLocalForwards = settings.LocalForwards == null || settings.LocalForwards.Length == 0;

        foreach (var item in block.Items)
            if (item is SshConfigEntry entry && handledKeys.Contains(entry.Key))
                switch (entry.Key.ToLowerInvariant())
                {
                    case "hostname":
                        if (!addedHostName)
                        {
                            newItems.Add(SshConfigEntry.Create("HostName", settings.HostName!));
                            addedHostName = true;
                        }

                        break;
                    case "user":
                        if (!addedUser)
                        {
                            newItems.Add(SshConfigEntry.Create("User", settings.User!));
                            addedUser = true;
                        }

                        break;
                    case "port":
                        if (!addedPort)
                        {
                            newItems.Add(SshConfigEntry.Create("Port", settings.Port.ToString()!));
                            addedPort = true;
                        }

                        break;
                    case "identityfile":
                        if (!addedIdentityFiles && settings.IdentityFiles != null)
                        {
                            foreach (var id in settings.IdentityFiles)
                                newItems.Add(SshConfigEntry.Create("IdentityFile", id));
                            addedIdentityFiles = true;
                        }

                        break;
                    case "proxyjump":
                        if (!addedProxyJump)
                        {
                            newItems.Add(SshConfigEntry.Create("ProxyJump", settings.ProxyJump!));
                            addedProxyJump = true;
                        }

                        break;
                    case "localforward":
                        if (!addedLocalForwards && settings.LocalForwards != null)
                        {
                            foreach (var lf in settings.LocalForwards)
                                newItems.Add(SshConfigEntry.Create("LocalForward", lf.Split(' ')));
                            addedLocalForwards = true;
                        }

                        break;
                }
            else if (item is SshConfigEntry otherEntry && settings.OtherEntries != null &&
                     settings.OtherEntries.Length > 0 && settings.OtherEntries.Contains(otherEntry))
                newItems.Add(item);
            else if (item is not SshConfigEntry) newItems.Add(item);

        // Add any settings that weren't in the original block
        if (!addedHostName) newItems.Add(SshConfigEntry.Create("HostName", settings.HostName!));
        if (!addedUser) newItems.Add(SshConfigEntry.Create("User", settings.User!));
        if (!addedPort) newItems.Add(SshConfigEntry.Create("Port", settings.Port.ToString()!));
        if (!addedProxyJump) newItems.Add(SshConfigEntry.Create("ProxyJump", settings.ProxyJump!));
        if (!addedIdentityFiles && settings.IdentityFiles != null)
            foreach (var id in settings.IdentityFiles)
                newItems.Add(SshConfigEntry.Create("IdentityFile", id));
        if (!addedLocalForwards && settings.LocalForwards != null)
            foreach (var lf in settings.LocalForwards)
                newItems.Add(SshConfigEntry.Create("LocalForward", lf.Split(' ')));

        // Add new other entries that weren't there
        if (settings.OtherEntries != null && settings.OtherEntries.Length > 0)
            foreach (var oe in settings.OtherEntries)
                if (!newItems.Contains(oe))
                    newItems.Add(oe);

        return block with { Items = newItems.ToImmutable(), RawHeaderText = string.Empty };
    }
}