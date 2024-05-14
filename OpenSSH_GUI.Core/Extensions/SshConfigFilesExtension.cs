#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 08.05.2024 - 22:05:30
// Last edit: 14.05.2024 - 03:05:38

#endregion

using OpenSSH_GUI.Core.Enums;

namespace OpenSSH_GUI.Core.Extensions;

public static class SshConfigFilesExtension
{
    private const string SshPathWithVariableLinux = "%HOME%/.ssh";
    private const string SshPathWithVariableWindows = "%USERPROFILE%\\.ssh";
    private const string SshRootPathLinux = "/etc/ssh";
    private const string SshRootPathWin = "%PROGRAMDATA%\\ssh";

    public static string GetRootSshPath(bool resolve = true, PlatformID? platformId = null)
    {
        var path = (platformId ?? Environment.OSVersion.Platform) switch
        {
            PlatformID.Win32S or PlatformID.Win32Windows or PlatformID.Win32NT or PlatformID.WinCE => SshRootPathWin,
            PlatformID.Unix or PlatformID.MacOSX => SshRootPathLinux,
            _ => throw new NotSupportedException(
                $"Platform {Environment.OSVersion.Platform.ToString().ToLower()} is not supported!")
        };
        return resolve ? Environment.ExpandEnvironmentVariables(path) : path;
    }

    public static string GetBaseSshPath(bool resolve = true, PlatformID? platformId = null)
    {
        var path = (platformId ?? Environment.OSVersion.Platform) switch
        {
            PlatformID.Win32S or PlatformID.Win32Windows or PlatformID.Win32NT or PlatformID.WinCE =>
                SshPathWithVariableWindows,
            PlatformID.Unix or PlatformID.MacOSX => SshPathWithVariableLinux,
            _ => throw new NotSupportedException(
                $"Platform {Environment.OSVersion.Platform.ToString().ToLower()} is not supported!")
        };
        return resolve ? Environment.ExpandEnvironmentVariables(path) : path;
    }

    public static string GetPathOfFile(this SshConfigFiles files, bool resolve = true, PlatformID? platform = null)
    {
        return Path.Combine(files switch
        {
            SshConfigFiles.Authorized_Keys or
                SshConfigFiles.Known_Hosts or
                SshConfigFiles.Config => GetBaseSshPath(resolve, platform),
            SshConfigFiles.Sshd_Config => GetRootSshPath(resolve, platform),
            _ => throw new ArgumentException("Invalid value for \"files\"")
        }, Enum.GetName(files)!.ToLower());
    }
}