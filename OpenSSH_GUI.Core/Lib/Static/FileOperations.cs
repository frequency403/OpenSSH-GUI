#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 30.05.2024 - 12:05:06
// Last edit: 30.05.2024 - 12:05:06

#endregion

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Extensions;

namespace OpenSSH_GUI.Core.Lib.Static;

public static class FileOperations
{
    public static void EnsureFilesAndFoldersExist(ILogger? logger = null)
    {
        logger??= NullLogger.Instance;
        foreach (var configFile in Enum.GetValues<SshConfigFiles>())
        {
            var pathOfFile = configFile.GetPathOfFile();
            try
            {
                if (!File.Exists(pathOfFile)) OpenOrCreate(pathOfFile);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error creating file {pathOfFile}", pathOfFile);
            }
        }
        SshConfigFilesExtension.ValidateDirectories(logger);
    }

    private static FileStreamOptions Options(FileMode mode)
    {
        var options = new FileStreamOptions
        {
            Access = FileAccess.ReadWrite,
            Mode = mode,
            Share = FileShare.ReadWrite
        };
        if (Environment.OSVersion.Platform is PlatformID.Unix or PlatformID.MacOSX)
        {
#pragma warning disable CA1416
            options.UnixCreateMode = mode
                is FileMode.Create
                or FileMode.CreateNew
                or FileMode.OpenOrCreate
                ? UnixFileMode.UserRead | UnixFileMode.UserWrite
                : null;
#pragma warning restore CA1416
        }

        return options;
    }

    public static FileStream DeleteOldAndCreateNew(string filePath)
    {
        if (Exists(filePath)) Delete(filePath);
        return OpenOrCreate(filePath);
    }

    public static FileStream OpenOrCreate(string filePath)
    {
        return new FileStream(filePath, Options(FileMode.OpenOrCreate));
    }

    public static FileStream OpenTruncated(string filePath)
    {
        return File.Open(filePath, Options(FileMode.Truncate));
    }

    public static string[] ReadAllLines(string filePath)
    {
        return File.ReadAllLines(filePath);
    }

    public static bool Exists(string filePath)
    {
        return File.Exists(filePath);
    }

    public static void Move(string source, string destination, bool overwrite = false)
    {
        File.Move(source, destination, overwrite);
    }

    public static void Delete(string filePath)
    {
        if (File.Exists(filePath)) File.Delete(filePath);
    }
}