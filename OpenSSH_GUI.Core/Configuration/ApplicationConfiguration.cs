using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Extensions;
using Serilog.Events;

namespace OpenSSH_GUI.Core.Configuration;

public class ApplicationConfiguration
{
    [JsonIgnore]
    public static string ApplicationConfigurationPath { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        AppDomain.CurrentDomain.FriendlyName);

    [JsonIgnore]
    public static string ApplicationConfigurationName { get; } = Path.WithJsonExtension(AppDomain.CurrentDomain.FriendlyName.ToLower());

    [JsonIgnore]
    public static string DefaultApplicationConfigurationFileFullPath { get; } = Path.Combine(ApplicationConfigurationPath, ApplicationConfigurationName);

    [JsonIgnore]
    public static readonly ApplicationConfiguration Default = new()
    {
        LookupPaths = [SshConfigFilesExtension.GetBaseSshPath()],
        PreferredTheme = ThemeVariant.Default,
        LogLevel = LogEventLevel.Warning,
        FontSize = 14,
        LoggerConfiguration = LoggerConfiguration.Default,
    };

    [Required]
    public required string[] LookupPaths { get; set; }
    
    [Required]
    public required ThemeVariant PreferredTheme { get; set; }
    
    [Required]
    public required LogEventLevel LogLevel { get; set; }
    
    [Required, Range(12, 48, ErrorMessage = "Font size must be between 12 and 48")]
    public required double FontSize { get; set; }

    [Required, ValidateObjectMembers]
    public required LoggerConfiguration LoggerConfiguration { get; set; }
}

[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true),JsonSerializable(typeof(ApplicationConfiguration)), JsonSerializable(typeof(LoggerConfiguration))]
public partial class SourceGenerationContext : JsonSerializerContext { }