using System.Reflection;
using System.Text;

namespace OpenSSH_GUI.Core.Lib.Config;

/// <summary>
/// A static class for parsing and writing SSH configuration files.
/// This parser supports comments, indentation, and preserves the order of host entries.
/// </summary>
public static class SshConfigParser
{
    /// <summary>
    /// Parses an SSH configuration from a given string content.
    /// </summary>
    /// <param name="configContent">The full string content of the SSH config file.</param>
    /// <returns>An SshConfig object representing the parsed configuration.</returns>
    public static SshConfig Parse(string configContent)
    {
        var config = new SshConfig();
        SshHostEntry? currentEntry = null;

        using var reader = new StringReader(configContent);
        while (reader.ReadLine() is { } line)
        {
            var trimmedLine = line.Trim();
            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith('#'))
            {
                // Ignore empty lines and comments
                continue;
            }

            // Split the line into keyword and value
            var parts = trimmedLine.Split([' ', '\t'], 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
            {
                // Invalid line format, skip
                continue;
            }

            var keyword = parts[0];
            var value = parts[1];

            if (string.Equals(keyword, "Host", StringComparison.OrdinalIgnoreCase))
            {
                // A new host entry begins
                currentEntry = new SshHostEntry { Host = value };
                config.HostEntries.Add(currentEntry);
            }
            else if (currentEntry != null)
            {
                // This is a property for the current host entry
                AssignProperty(currentEntry, keyword, value);
            }
        }

        return config;
    }

    /// <summary>
    /// Writes an SshConfig object to a string in the correct SSH config format.
    /// </summary>
    /// <param name="config">The SshConfig object to write.</param>
    /// <returns>A string representing the content of an SSH config file.</returns>
    public static string Write(SshConfig config)
    {
        var sb = new StringBuilder();
        foreach (var entry in config.HostEntries)
        {
            sb.AppendLine($"Host {entry.Host}");

            // Write all non-null properties using reflection
            var properties = entry.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties.Where(p => p.Name != "Host" && p.Name != "AdditionalProperties"))
            {
                var value = prop.GetValue(entry);
                if (value != null)
                {
                    sb.AppendLine($"    {prop.Name} {value}");
                }
            }

            // Write additional properties
            foreach (var kvp in entry.AdditionalProperties)
            {
                sb.AppendLine($"    {kvp.Key} {kvp.Value}");
            }
            sb.AppendLine(); // Add a blank line for readability
        }
        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Private helper to assign a value to a property of an SshHostEntry.
    /// It uses reflection to find a matching property or adds it to the AdditionalProperties dictionary.
    /// </summary>
    private static void AssignProperty(SshHostEntry entry, string keyword, string value)
    {
        var property = entry.GetType().GetProperty(keyword, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

        if (property != null && property.CanWrite)
        {
            try
            {
                // Handle nullable integer properties
                if (property.PropertyType == typeof(int?))
                {
                    if (int.TryParse(value, out var intValue))
                    {
                        property.SetValue(entry, intValue);
                    }
                }
                else
                {
                    // Handle string properties
                    property.SetValue(entry, value);
                }
            }
            catch (ArgumentException)
            {
                // Fallback for type conversion errors
                entry.AdditionalProperties[keyword] = value;
            }
        }
        else
        {
            // If no direct property exists, store it in the dictionary
            entry.AdditionalProperties[keyword] = value;
        }
    }
}