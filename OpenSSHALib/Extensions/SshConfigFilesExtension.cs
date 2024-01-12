using System.Linq.Expressions;
using OpenSSHALib.Enums;
using OpenSSHALib.Lib;

namespace OpenSSHALib.Extensions;

public static class SshConfigFilesExtension
{
    private const string SshPathWithVariableLinux = "$HOME/.ssh";
    private const string SshPathWithVariableWindows = "%USERPROFILE%\\.ssh";
    private const string SshRootPathLinux = "/etc/ssh";
    private const string SshRootPathWin = "%PROGRAMDATA%\\ssh";

    private const char DirSeparatorWin = '\\';
    private const char DirSeparatorLinux = '/';

    public static string GetRootSshPath(PlatformID? platformId = null)
    {
        return Environment.ExpandEnvironmentVariables((platformId ?? Environment.OSVersion.Platform) switch
        {
            PlatformID.Win32S or PlatformID.Win32Windows or PlatformID.Win32NT or PlatformID.WinCE => SshRootPathWin,
            PlatformID.Unix or PlatformID.MacOSX => SshRootPathLinux,
            _ => throw new NotSupportedException(
                $"Platform {Environment.OSVersion.Platform.ToString().ToLower()} is not supported!")
        });
    }
    
    public static string GetBaseSshPath(PlatformID? platformId = null)
    {
        return Environment.ExpandEnvironmentVariables((platformId ?? Environment.OSVersion.Platform) switch
        {
            PlatformID.Win32S or PlatformID.Win32Windows or PlatformID.Win32NT or PlatformID.WinCE => SshPathWithVariableWindows,
            PlatformID.Unix or PlatformID.MacOSX => SshPathWithVariableLinux,
            _ => throw new NotSupportedException(
                $"Platform {Environment.OSVersion.Platform.ToString().ToLower()} is not supported!")
        });
    } 
    
    public static string GetPathOfFile(this SshConfigFiles files, PlatformID? platform = null)
    {
        return Environment.ExpandEnvironmentVariables((platform ?? Environment.OSVersion.Platform) switch
        {
            PlatformID.Win32S or PlatformID.Win32Windows or PlatformID.Win32NT or PlatformID.WinCE =>
                files switch
                {
                    SshConfigFiles.Authorized_Keys => $"{SshPathWithVariableWindows}{DirSeparatorWin}",
                    SshConfigFiles.Known_Hosts => $"{SshPathWithVariableWindows}{DirSeparatorWin}",
                    SshConfigFiles.Config => $"{SshPathWithVariableWindows}{DirSeparatorWin}",
                    SshConfigFiles.Sshd_Config => $"{SshRootPathWin}{DirSeparatorWin}",
                    _ => throw new ArgumentException("Invalid value for \"files\"")
                },
            PlatformID.Unix or PlatformID.MacOSX =>
                files switch
                {
                    SshConfigFiles.Authorized_Keys => $"{SshPathWithVariableLinux}{DirSeparatorLinux}",
                    SshConfigFiles.Known_Hosts => $"{SshPathWithVariableLinux}{DirSeparatorLinux}",
                    SshConfigFiles.Config => $"{SshPathWithVariableLinux}{DirSeparatorLinux}",
                    SshConfigFiles.Sshd_Config => $"{SshRootPathLinux}{DirSeparatorLinux}",
                    _ => throw new ArgumentException("Invalid value for \"files\"")
                },
            _ =>
                throw new NotSupportedException(
                    $"Platform {Environment.OSVersion.Platform.ToString().ToLower()} is not supported!")
        }) + Enum.GetName(files).ToLower();
    }
}