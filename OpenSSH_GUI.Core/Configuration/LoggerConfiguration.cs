using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using OpenSSH_GUI.Core.Extensions;

namespace OpenSSH_GUI.Core.Configuration;

public record LoggerConfiguration
{
#if DEBUG
    private const string LogTemplate =
        "[{Timestamp:yyyy/MM/dd HH:mm:ss}] [{Level:u3}] ({FileName}:{LineNumber}): {Message:lj}{NewLine}{Exception}";
#else
    private const string LogTemplate =
        "[{Timestamp:yyyy/MM/dd HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}";
#endif

    [Required(AllowEmptyStrings = false)]
    public string LogFileName { get; set; } = Path.WithLogExtension(AppDomain.CurrentDomain.FriendlyName);

    [Required(AllowEmptyStrings = false)]
    public string LogFilePath { get; set; } =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppDomain.CurrentDomain.FriendlyName, PathExtensions.LogExtension);

    public string LogOutputTemplate { get; set; } = LogTemplate;

    [JsonIgnore]
    public string LogFileFullPath => Path.Combine(LogFilePath, LogFileName);
    
    [JsonIgnore]
    public static LoggerConfiguration Default { get; } = new();
}