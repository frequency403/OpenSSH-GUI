using OpenSSHALib.Models;

namespace OpenSSHALib.Lib;

public static class DirectoryCrawler
{
    private static readonly IEnumerable<string> _fileNameContainsToSkipWhenSearching =
        ["authorized", "config", "known"];

    private static bool FileNameStartsWithAny(this string fullFilePath, IEnumerable<string> collection)
    {
        var contains = false;

        foreach (var phrase in collection)
        {
            if (contains) break;
            if (Path.GetFileName(fullFilePath).StartsWith(phrase)) contains = true;
        }

        return contains;
    }

    public static IEnumerable<SshKey> GetAllKeys()
    {
        return (from fileInSshDirectory in Directory.EnumerateFiles(
                Settings.UserSshFolderPath)
            where !fileInSshDirectory.FileNameStartsWithAny(_fileNameContainsToSkipWhenSearching) &&
                  fileInSshDirectory.EndsWith(".pub")
            select new SshKey(fileInSshDirectory)).ToList();
    }
}