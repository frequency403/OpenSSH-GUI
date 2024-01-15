using OpenSSHALib.Enums;
using OpenSSHALib.Extensions;

namespace OpenSSHALib.Lib;

public static class InitializationRoutine
{
    private static bool IsProgramStartReady => Directory.Exists(SshConfigFilesExtension.GetBaseSshPath()) &&
                                               File.Exists(SshConfigFiles.Known_Hosts.GetPathOfFile());

    public static bool MakeProgramStartReady()
    {
        try
        {
            if (IsProgramStartReady) return IsProgramStartReady;
            if (SettingsFileHandler.IsFileInitialized)
            {
                if (!Directory.Exists(SshConfigFilesExtension.GetBaseSshPath()))
                    Directory.CreateDirectory(SshConfigFilesExtension.GetBaseSshPath());
                if (!File.Exists(SshConfigFiles.Known_Hosts.GetPathOfFile()))
                {
                    using var file = File.Create(SshConfigFiles.Known_Hosts.GetPathOfFile());
                }
            }

            if (!IsProgramStartReady) MakeProgramStartReady();
        }
        catch (Exception d)
        {
            Console.WriteLine(d);
            return IsProgramStartReady;
        }

        return IsProgramStartReady;
    }
}