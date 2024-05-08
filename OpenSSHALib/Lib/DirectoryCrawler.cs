#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 08.05.2024 - 22:05:30
// Last edit: 08.05.2024 - 22:05:05

#endregion

using Microsoft.Extensions.Logging;
using OpenSSHALib.Extensions;
using OpenSSHALib.Interfaces;
using OpenSSHALib.Models;

namespace OpenSSHALib.Lib;

public class DirectoryCrawler(ILogger<DirectoryCrawler> logger, IApplicationSettings settings)
{
    public IEnumerable<ISshKey?> GetAllKeys()
    {
        foreach (var filePath in Directory
                     .EnumerateFiles(SshConfigFilesExtension.GetBaseSshPath(), "*", SearchOption.TopDirectoryOnly))
        {
            ISshKey? key = null;
            try
            {
                var extension = Path.GetExtension(filePath);
                
                if (extension.EndsWith(".pub")) key = new SshPublicKey(filePath);
                if (extension.EndsWith(".ppk")) key = settings.Settings.ConvertPpkAutomatically ? new PpkKey(filePath).ConvertToOpenSshKey(out _) : new PpkKey(filePath);
                
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while reading public key from {path}", filePath);
            }
            if(key is null) continue;
            yield return key;
        }
    }
}