#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 08.05.2024 - 22:05:30
// Last edit: 14.05.2024 - 03:05:31

#endregion

using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Interfaces.Keys;
using OpenSSH_GUI.Core.Interfaces.Settings;
using OpenSSH_GUI.Core.Lib.Keys;
using PpkKey = OpenSSH_GUI.Core.Lib.Keys.PpkKey;

namespace OpenSSH_GUI.Core.Lib.Misc;

public class DirectoryCrawler(ILogger logger, ISettingsFile settings)
{
    private IEnumerable<ISshKey> Cache = [];
    
    public IEnumerable<ISshKey> Refresh() => GetAllKeys(true);

    public ISettingsFile SettingsFile
    {
        get => settings;
        set
        {
            settings = value;
            Refresh();
        }
    }

    public IEnumerable<ISshKey> GetAllKeys(bool loadFromDisk = false)
    {
        if (!loadFromDisk && Cache.Any())
        {
            foreach (var key in Cache)
            {
                yield return key;
            }
            yield break;
        }

        if (Cache.Any()) Cache = [];
        foreach (var filePath in Directory
                     .EnumerateFiles(SshConfigFilesExtension.GetBaseSshPath(), "*", SearchOption.TopDirectoryOnly))
        {
            ISshKey? key = null;
            var message = "";
            try
            {
                var extension = Path.GetExtension(filePath);

                if (extension.EndsWith(".pub")) key = new SshPublicKey(filePath);
                if (extension.EndsWith(".ppk"))
                    key = SettingsFile.ConvertPpkAutomatically
                        ? new PpkKey(filePath).ConvertToOpenSshKey(out message)
                        : new PpkKey(filePath);
                if (!string.IsNullOrWhiteSpace(message)) throw new InvalidOperationException(message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while reading public key from {path}", filePath);
            }

            if (key is null) continue;
            Cache = Cache.Append(key);
            yield return key;
        }
    }
}