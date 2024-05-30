#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 30.05.2024 - 11:05:42
// Last edit: 30.05.2024 - 11:05:42

#endregion

using System.Diagnostics;
using OpenSSH_GUI.Core.Enums;

namespace OpenSSH_GUI.Core.Extensions;

public static class FileStreamExtensions
{
    public static async Task<FileStream> WithUnixPermissionsAsync(this FileStream stream, UnixPermissions permission)
    {
        if (Environment.OSVersion.Platform is PlatformID.Unix or PlatformID.MacOSX)
        {
            using var proc = new Process();
            proc.StartInfo.FileName = "/bin/bash";
            proc.StartInfo.ArgumentList.Add("-c");
            proc.StartInfo.ArgumentList.Add($"chmod {(int)permission} {stream.Name}");
            proc.Start();
            await proc.WaitForExitAsync();
            if (proc.ExitCode != 0)
                throw new InvalidOperationException($"Chmod execution failed with exit code : {proc.ExitCode}");
        }

        return stream;
    }

    public static FileStream WithUnixPermissions(this FileStream stream, UnixPermissions permission)
    {
        return stream.WithUnixPermissionsAsync(permission).GetAwaiter().GetResult();
    }
}