using System.Text.Json.Serialization;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Extensions;

namespace OpenSSH_GUI.Core.Configuration;

public class ApplicationConfiguration()
{
    [JsonIgnore]
    public static readonly string ApplicationConfigurationPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        AppDomain.CurrentDomain.FriendlyName);

    [JsonIgnore]
    public static readonly string ApplicationConfigurationName = Path.ChangeExtension(AppDomain.CurrentDomain.FriendlyName.ToLower(), "json");

    [JsonIgnore]
    public static readonly string DefaultApplicationConfigurationFileFullPath = Path.Combine(ApplicationConfigurationPath, ApplicationConfigurationName);

    [JsonIgnore]
    public static readonly ApplicationConfiguration Default = new()
    {
        LookupPaths = [SshConfigFilesExtension.GetBaseSshPath()],
        PreferredTheme = ThemeVariant.Default,
        FontSize = 14,
        LoggerConfiguration = LoggerConfiguration.Default,
    };

    public string[] LookupPaths { get; set; }

    public ThemeVariant PreferredTheme { get; set; }

    public int FontSize { get; set; }

    public LoggerConfiguration LoggerConfiguration { get; set; }
}

[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true),JsonSerializable(typeof(ApplicationConfiguration)), JsonSerializable(typeof(LoggerConfiguration))]
public partial class SourceGenerationContext : JsonSerializerContext { }