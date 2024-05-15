#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:27

#endregion

using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Interfaces.Keys;
using OpenSSH_GUI.Core.Interfaces.Settings;
using OpenSSH_GUI.Core.Lib.Keys;
using SshNet.Keygen;
using PpkKey = OpenSSH_GUI.Core.Lib.Keys.PpkKey;

namespace OpenSSH_GUI.Core.Lib.Misc;

public class DirectoryCrawler(ILogger logger, ISettingsFile settings)
{
    private IEnumerable<ISshKey> Cache = [];

    public ISettingsFile SettingsFile
    {
        get => settings;
        set
        {
            settings = value;
            Refresh();
        }
    }

    public IEnumerable<ISshKey> Refresh()
    {
        return GetAllKeys(true);
    }

    public IEnumerable<ISshKey> GetAllKeys(bool loadFromDisk = false)
    {
        if (!loadFromDisk && Cache.Any())
        {
            foreach (var key in Cache) yield return key;
            yield break;
        }

        if (Cache.Any()) Cache = [];
        foreach (var filePath in Directory
                     .EnumerateFiles(SshConfigFilesExtension.GetBaseSshPath(), "*", SearchOption.TopDirectoryOnly))
        {
            ISshKey? key = null;
            try
            {
                var extension = Path.GetExtension(filePath);

                if (extension.EndsWith(".pub")) key = new SshPublicKey(filePath);
                if (extension.EndsWith(".ppk"))
                {
                    key = new PpkKey(filePath);
                    if (SettingsFile.ConvertPpkAutomatically) key = key.Convert(SshKeyFormat.OpenSSH, true, logger);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while reading key from {path}", filePath);
            }

            if (key is null) continue;
            Cache = Cache.Append(key);
            yield return key;
        }
    }
}