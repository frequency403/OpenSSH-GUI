﻿using OpenSSHALib.Models;

namespace OpenSSHALib.Lib;

public static class DirectoryCrawler
{
    private static bool FileNameStartsWithAny(this string fullFilePath, params string[] collection)
    {
        var contains = false;

        foreach (var phrase in collection)
        {
            if (contains) break;
            if (Path.GetFileName(fullFilePath).StartsWith(phrase)) contains = true;
        }

        return contains;
    }

    public static IEnumerable<SshPublicKey> GetAllKeys()
    {
        var list = new List<SshPublicKey>();
        foreach (var filepath in Directory.EnumerateFiles(
                     SettingsFileHandler.Settings.UserSshFolderPath).Where(e =>
                     !e.FileNameStartsWithAny(SettingsFileHandler.Settings.FileNamesToSkipWhenSearchingForKeys) &&
                     e.EndsWith(".pub")))
        {
            var key = new SshPublicKey(filepath);
            key.GetPrivateKey();
            list.Add(key);
        }

        return list;
    }
}