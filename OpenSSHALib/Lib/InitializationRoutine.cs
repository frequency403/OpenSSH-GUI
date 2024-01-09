namespace OpenSSHALib.Lib;

public static class InitializationRoutine
{
    public static bool IsProgramStartReady => Directory.Exists(SettingsFileHandler.Settings.UserSshFolderPath) &&
                                              File.Exists(SettingsFileHandler.Settings.KnownHostsFilePath);
    
    public static bool MakeProgramStartReady()
    {
        try
        {
            if (IsProgramStartReady) return IsProgramStartReady;
            if (SettingsFileHandler.IsFileInitialized)
            {
                if (!Directory.Exists(SettingsFileHandler.Settings.UserSshFolderPath))
                    Directory.CreateDirectory(SettingsFileHandler.Settings.UserSshFolderPath);
                if (!File.Exists(SettingsFileHandler.Settings.KnownHostsFilePath))
                {
                    using var file = File.Create(SettingsFileHandler.Settings.KnownHostsFilePath);
                }
            }

            if (!IsProgramStartReady) MakeProgramStartReady();
        }
        catch (Exception)
        {
            return IsProgramStartReady;
        }
        return IsProgramStartReady; 
    }
}