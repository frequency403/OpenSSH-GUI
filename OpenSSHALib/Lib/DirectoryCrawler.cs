using OpenSSHALib.Model;

namespace OpenSSHALib.Lib;

public static class DirectoryCrawler
{
    private static IEnumerable<string> _fileNameContainsToSkipWhenSearching = ["authorized", "config", "known"];

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
    
    public static IEnumerable<SSHKey> GetAllKeys()
    {
        return (from fileInSshDirectory in Directory.EnumerateFiles(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + $"{Path.DirectorySeparatorChar}.ssh") where !fileInSshDirectory.FileNameStartsWithAny(_fileNameContainsToSkipWhenSearching) && fileInSshDirectory.EndsWith(".pub") select new SSHKey(fileInSshDirectory)).ToList();
    }
}