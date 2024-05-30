#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 30.05.2024 - 12:05:06
// Last edit: 30.05.2024 - 12:05:06

#endregion

namespace OpenSSH_GUI.Core.Lib.Static;

public static class FileOperations
{
    private static FileStreamOptions Options(FileMode mode)
    {
        return new FileStreamOptions
        {
            Access = FileAccess.ReadWrite,
            Mode = mode,
            Share = FileShare.ReadWrite,
#pragma warning disable CA1416
            UnixCreateMode = mode
                is FileMode.Create
                or FileMode.CreateNew
                or FileMode.OpenOrCreate
                ? UnixFileMode.UserRead | UnixFileMode.UserWrite
                : null
#pragma warning restore CA1416
        };
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