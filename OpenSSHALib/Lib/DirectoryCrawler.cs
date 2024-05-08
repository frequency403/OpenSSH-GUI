using Microsoft.Extensions.Logging;
using OpenSSHALib.Extensions;
using OpenSSHALib.Interfaces;
using OpenSSHALib.Lib.Structs;
using OpenSSHALib.Models;
using SshNet.PuttyKeyFile;

namespace OpenSSHALib.Lib;

public class DirectoryCrawler(ILogger<DirectoryCrawler> logger, IApplicationSettings settings)
{
    public IEnumerable<ISshKey?> GetAllKeys(out List<SshCrawlError> errors)
    {
        var errorList = new List<SshCrawlError>();
        errors = errorList;
        
        var sshKeyList =Directory.EnumerateFiles(SshConfigFilesExtension.GetBaseSshPath(), "*.pub", SearchOption.AllDirectories)
            .Select(filePath =>
            {
                try
                {
                    return new SshPublicKey(filePath) as ISshKey;
                }
                catch (Exception ex)
                {
                    errorList.Add(new SshCrawlError(filePath, ex));
                    logger.LogError(ex, "Error while reading public key from {path}", filePath);
                    return null;
                }
            }).ToList();
            
            sshKeyList.AddRange(Directory.EnumerateFiles(SshConfigFilesExtension.GetBaseSshPath(), "*.ppk", SearchOption.TopDirectoryOnly)
                    .Select(filePath =>
                    {
                        try
                        {
                            ISshKey? key = new PpkKey(filePath).ConvertToOpenSshKey(out string error, !settings.Settings.ConvertPpkAutomatically);
                            if (key is null || !string.IsNullOrWhiteSpace(error)) throw new Exception(error);
                            return key;
                        }
                        catch (Exception ex)
                        {
                            errorList.Add(new SshCrawlError(filePath, ex));
                            logger.LogError(ex, "Error while converting PPK to OpenSshKey from {path}", filePath);
                            return null;
                        }
                    }).ToList());
        return sshKeyList.Where(x => x != null);
    }
}