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
        
        var sshKeyList =Directory.EnumerateFiles(SshConfigFilesExtension.GetBaseSshPath(), "*.pub", SearchOption.AllDirectories)
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
            }).ToList();
            sshKeyList.AddRange(Directory.EnumerateFiles(SshConfigFilesExtension.GetBaseSshPath(), "*.ppk", SearchOption.TopDirectoryOnly)
                    .Select(filePath =>
                    {
                        try
                        {
                            var key = new PpkKey(filePath).ConvertToOpenSshKey(out string error);
                            if (!string.IsNullOrWhiteSpace(error)) throw new Exception(error);
                            return key;
                        }
                        catch (Exception ex)
                        {
                            errorList.Add(new SshCrawlError(filePath, ex));
                            return null;
                        }
                    }).ToList());
        return sshKeyList.Where(x => x != null);
    }
}