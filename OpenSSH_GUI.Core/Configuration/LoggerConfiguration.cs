namespace OpenSSH_GUI.Core.Configuration;

public record LoggerConfiguration
{
    private const string LogFileFolderAndExtension = "log";

    public string LogFileName { get; set; } =
        Path.ChangeExtension(AppDomain.CurrentDomain.FriendlyName, LogFileFolderAndExtension);
    
    public string LogFilePath { get; set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppDomain.CurrentDomain.FriendlyName, LogFileFolderAndExtension);

    public string LogFileFullPath => Path.Combine(LogFilePath, LogFileName);
    
    public static LoggerConfiguration Default { get; } = new();
}