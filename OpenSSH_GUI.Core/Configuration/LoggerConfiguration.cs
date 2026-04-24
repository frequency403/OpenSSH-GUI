namespace OpenSSH_GUI.Core.Configuration;

public record LoggerConfiguration
{
    private const string LogFileFolderAndExtension = "log";

#if DEBUG
    private const string LogTemplate =
        "[{Timestamp:yyyy/MM/dd HH:mm:ss}] [{Level:u3}] ({FileName}:{LineNumber}): {Message:lj}{NewLine}{Exception}";
#else
    private const string LogTemplate =
        "[{Timestamp:yyyy/MM/dd HH:mm:ss}] [{Level:u3}] {Message:lj}{NewLine}{Exception}";
#endif

    public string LogFileName { get; set; } =
        Path.ChangeExtension(AppDomain.CurrentDomain.FriendlyName, LogFileFolderAndExtension);

    public string LogFilePath { get; set; } =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppDomain.CurrentDomain.FriendlyName, LogFileFolderAndExtension);

    public string LogFileFullPath => Path.Combine(LogFilePath, LogFileName);

    public string LogOutputTemplate => LogTemplate;

    public static LoggerConfiguration Default { get; } = new();
}