using OpenSSHALib.Extensions;
using OpenSSHALib.Lib.Structs;
using OpenSSHALib.Models;

namespace OpenSSHALib.Lib;

public static class DirectoryCrawler
{
    public static IEnumerable<SshPublicKey> GetAllKeys(out List<SshCrawlError> errors)
    {
        var errorList = new List<SshCrawlError>();
        errors = errorList;
        return Directory.EnumerateFiles(SshConfigFilesExtension.GetBaseSshPath(), "*.pub", SearchOption.AllDirectories)
            .Select(filePath =>
            {
                try
                {
                    return new SshPublicKey(filePath);
                }
                catch (Exception ex)
                {
                    errorList.Add(new SshCrawlError(filePath, ex));
                    return null;
                }
            })
            .Where(x => x != null)!;
    }
}