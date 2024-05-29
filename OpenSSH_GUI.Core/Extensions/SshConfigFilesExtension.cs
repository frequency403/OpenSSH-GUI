#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:25

#endregion

using OpenSSH_GUI.Core.Enums;

namespace OpenSSH_GUI.Core.Extensions;

/// <summary>
/// Provides extension methods for handling SSH configuration files.
/// </summary>
public static class SshConfigFilesExtension
{
    /// <summary>
    /// Represents the path for SSH with variable on Linux.
    /// </summary>
    private const string SshPathWithVariableLinux = "%HOME%/.ssh";

    /// <summary>
    /// Represents the SSH path with a variable for Windows operating system.
    /// </summary>
    private const string SshPathWithVariableWindows = "%USERPROFILE%\\.ssh";

    /// <summary>
    /// Represents the root SSH path for Linux.
    /// </summary>
    private const string SshRootPathLinux = "/etc/ssh";

    /// <summary>
    /// The SSH root path on Windows.
    /// </summary>
    private const string SshRootPathWin = "%PROGRAMDATA%\\ssh";

    public static void ValidateDirectories()
    {
        switch (Environment.OSVersion.Platform)
        {
            case PlatformID.Win32S:
            case PlatformID.Win32Windows:
            case PlatformID.Win32NT:
            case PlatformID.WinCE:
                var sshPathWin = Environment.ExpandEnvironmentVariables(SshPathWithVariableWindows);
                var sshConfigPathWin = Environment.ExpandEnvironmentVariables(SshRootPathWin);
                if (!Directory.Exists(sshPathWin)) Directory.CreateDirectory(sshPathWin);
                try
                {
                    if (!Directory.Exists(sshConfigPathWin)) Directory.CreateDirectory(sshConfigPathWin);
                }
                catch (Exception e)
                {
                    //
                }
                break;
            case PlatformID.Unix:
            case PlatformID.MacOSX:
                var sshPath = Environment.ExpandEnvironmentVariables(SshPathWithVariableLinux);
                if (!Directory.Exists(sshPath)) Directory.CreateDirectory(sshPath);
                try
                {
                    if (!Directory.Exists(SshRootPathLinux)) Directory.CreateDirectory(SshRootPathLinux);
                }
                catch (Exception e)
                {
                    //
                }
                break;
            case PlatformID.Other:
            case PlatformID.Xbox:
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    /// <summary>
    /// Retrieves the root SSH path based on the platform.
    /// </summary>
    /// <param name="resolve">Indicates whether to resolve environment variables in the path. Defaults to true.</param>
    /// <param name="platformId">The platform ID. Defaults to the current operating system platform.</param>
    /// <returns>The root SSH path.</returns>
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

    /// Gets the base path to the SSH directory.
    /// @param resolve Boolean value indicating whether to resolve environment variables in the path. Default is true.
    /// @param platformId (optional) The platform ID. Default is the platform of the current environment.
    /// @returns The base path to the SSH directory.
    /// /
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

    /// <summary>
    /// Retrieves the file path corresponding to the specified <see cref="SshConfigFiles"/> enum value.
    /// </summary>
    /// <param name="files">The <see cref="SshConfigFiles"/> enum value representing the SSH config file.</param>
    /// <param name="resolve">A boolean value indicating whether to resolve the file path using <see cref="GetBaseSshPath"/> or <see cref="GetRootSshPath"/>.</param>
    /// <param name="platform">The target platform identifier to determine the file path format.</param>
    /// <returns>The file path as a <see cref="string"/>.</returns>
    public static string GetPathOfFile(this SshConfigFiles files, bool resolve = true, PlatformID? platform = null)
    {
        var path = Path.Combine(files switch
        {
            SshConfigFiles.Authorized_Keys or
                SshConfigFiles.Known_Hosts or
                SshConfigFiles.Config => GetBaseSshPath(resolve, platform),
            SshConfigFiles.Sshd_Config => GetRootSshPath(resolve, platform),
            _ => throw new ArgumentException("Invalid value for \"files\"")
        }, Enum.GetName(files)!.ToLower());
        path = platform is PlatformID.Unix ? path.Replace('\\', '/') : path.Replace('/', '\\');
        return path;
    }
}