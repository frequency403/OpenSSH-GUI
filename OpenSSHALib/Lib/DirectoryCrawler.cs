using OpenSSHALib.Extensions;
using OpenSSHALib.Models;

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

    public static IEnumerable<SshPublicKey> GetAllKeys() => Directory.EnumerateFiles(SshConfigFilesExtension.GetBaseSshPath(), "*.pub", SearchOption.AllDirectories).Select(
        e =>
        {
            var key = new SshPublicKey(e);
            key.GetPrivateKey();
            return key;
        });
}