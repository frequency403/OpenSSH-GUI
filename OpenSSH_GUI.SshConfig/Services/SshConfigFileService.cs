using OpenSSH_GUI.SshConfig.Extensions;
using OpenSSH_GUI.SshConfig.Models;
using OpenSSH_GUI.SshConfig.Options;
using OpenSSH_GUI.SshConfig.Parsers;
using OpenSSH_GUI.SshConfig.Serializers;

namespace OpenSSH_GUI.SshConfig.Services;

/// <summary>
///     Parses an SSH configuration file directly into an <see cref="SshConfiguration" /> object,
///     bypassing the IConfiguration pipeline entirely.
/// </summary>
public static class SshConfigFileService
{
    /// <summary>
    ///     Reads and parses the specified SSH configuration file.
    /// </summary>
    /// <param name="filePath">Absolute path to the SSH config file.</param>
    /// <returns>A fully populated <see cref="SshConfiguration" />, or an empty one if the file doesn't exist.</returns>
    public static SshConfiguration LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
            return new SshConfiguration();

        var content = File.ReadAllText(filePath);
        var document = SshConfigParser.Parse(content, new SshConfigParserOptions
        {
            IncludeBasePath = Path.GetDirectoryName(filePath)
        });

        return MapDocumentToConfiguration(document);
    }

    /// <summary>
    ///     Maps a parsed <see cref="SshConfigDocument" /> to a bindable <see cref="SshConfiguration" />.
    /// </summary>
    public static SshConfiguration MapDocumentToConfiguration(SshConfigDocument document)
    {
        var config = new SshConfiguration
        {
            Global = SshHostBlockExtensions.GetSettingsFromEntries(
                document.GlobalItems.OfType<SshConfigEntry>())
        };

        foreach (var block in document.Blocks) config.Blocks.Add(block.GetSettings());

        foreach (var hostBlock in document.HostBlocks) config.Hosts.Add(hostBlock.GetSettings());

        return config;
    }
}