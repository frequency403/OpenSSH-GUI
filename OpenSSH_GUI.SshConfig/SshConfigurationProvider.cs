using Microsoft.Extensions.Configuration;

namespace OpenSSH_GUI.SshConfig;

/// <summary>
///     Provides SSH configuration data from an SSH client configuration file.
/// </summary>
public sealed class SshConfigurationProvider : FileConfigurationProvider
{
    /// <summary>
    ///     Initializes a new instance of <see cref="SshConfigurationProvider" />.
    /// </summary>
    /// <param name="source">The source settings.</param>
    public SshConfigurationProvider(SshConfigurationSource source) : base(source)
    {
    }

    /// <summary>
    ///     Loads the SSH configuration data from the stream.
    /// </summary>
    /// <param name="stream">The stream to read from.</param>
    public override void Load(Stream stream)
    {
        using var reader = new StreamReader(stream);
        var content = reader.ReadToEnd();

        // Use the existing parser to parse the content.
        // We use the file path from the source if available for better error messages.
        var filePath = Source.Path;
        var document = SshConfigParser.Parse(content,
            new SshConfigParserOptions { IncludeBasePath = filePath is null ? null : Path.GetDirectoryName(filePath) });

        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        // Map Global items as an object (instead of flat properties)
        var globalSettings =
            SshHostBlockExtensions.GetSettingsFromEntries(document.GlobalItems.OfType<SshConfigEntry>());
        MapSettings(data, "SshConfig:Global", globalSettings);

        // Map all Blocks in document order (crucial for SSH behavior)
        for (var i = 0; i < document.Blocks.Length; i++)
        {
            var block = document.Blocks[i];
            var prefix = $"SshConfig:Blocks:{i}";

            data[$"{prefix}:Type"] = block switch
            {
                SshHostBlock => "Host",
                SshMatchBlock => "Match",
                _ => "Unknown"
            };

            MapSettings(data, prefix, block.GetSettings());
        }

        // Map Host Blocks separately as a convenience list for binding to SshHostSettings
        var hostBlocks = document.HostBlocks.ToArray();
        for (var i = 0; i < hostBlocks.Length; i++)
            MapSettings(data, $"SshConfig:Hosts:{i}", hostBlocks[i].GetSettings());

        Data = data;
    }

    private static void MapSettings(Dictionary<string, string?> data, string prefix, SshHostSettings settings)
    {
        // Map patterns
        if (settings.Patterns is { Length: > 0 })
            for (var i = 0; i < settings.Patterns.Length; i++)
                data[$"{prefix}:Patterns:{i}"] = settings.Patterns[i];

        if (settings.HostName != null) data[$"{prefix}:HostName"] = settings.HostName;
        if (settings.User != null) data[$"{prefix}:User"] = settings.User;
        if (settings.Port.HasValue) data[$"{prefix}:Port"] = settings.Port.Value.ToString();
        if (settings.ProxyJump != null) data[$"{prefix}:ProxyJump"] = settings.ProxyJump;

        if (settings.IdentityFiles is { Length: > 0 })
            for (var i = 0; i < settings.IdentityFiles.Length; i++)
                data[$"{prefix}:IdentityFiles:{i}"] = settings.IdentityFiles[i];

        if (settings.LocalForwards is { Length: > 0 })
            for (var i = 0; i < settings.LocalForwards.Length; i++)
                data[$"{prefix}:LocalForwards:{i}"] = settings.LocalForwards[i];

        // Map other entries if any
        if (settings.OtherEntries is not { Length: > 0 }) return;
        {
            for (var i = 0; i < settings.OtherEntries.Length; i++)
            {
                var entry = settings.OtherEntries[i];
                data[$"{prefix}:OtherEntries:{i}:Key"] = entry.Key;
                data[$"{prefix}:OtherEntries:{i}:Value"] = entry.Value;
            }
        }
    }
}