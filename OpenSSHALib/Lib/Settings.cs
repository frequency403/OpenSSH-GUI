namespace OpenSSHALib.Lib;

public static class Settings
{
    public static string UserSshFolderPath => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) +
                                              $"{Path.DirectorySeparatorChar}.ssh";

    public static string KnownHostsFilePath => UserSshFolderPath + $"{Path.DirectorySeparatorChar}known_hosts";
}