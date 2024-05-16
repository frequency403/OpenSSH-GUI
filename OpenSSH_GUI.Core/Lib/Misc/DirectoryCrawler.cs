#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:27

#endregion

using DynamicData;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Interfaces.Credentials;
using OpenSSH_GUI.Core.Interfaces.Keys;
using OpenSSH_GUI.Core.Interfaces.Settings;
using OpenSSH_GUI.Core.Lib.Keys;
using OpenSSH_GUI.Core.Lib.Settings;
using SshNet.Keygen;
using PpkKey = OpenSSH_GUI.Core.Lib.Keys.PpkKey;

namespace OpenSSH_GUI.Core.Lib.Misc;

public class DirectoryCrawler(ILogger<DirectoryCrawler> logger, ISettingsFile settingsFile)
{
    private IEnumerable<ISshKey> _cache = [];

    public IEnumerable<ISshKey> Refresh()
    {
        return GetAllKeys(true);
    }

    public void UpdateKey(ISshKey sshKey)
    {
        var cacheList = _cache.ToList();
        var cachedKey = cacheList.FirstOrDefault(e => e.AbsoluteFilePath == sshKey.AbsoluteFilePath);
        if (cachedKey is null)
        {
            cacheList.Add(sshKey);
            _cache = cacheList;
            return;
        }
        var index = cacheList.IndexOf(cachedKey);
        cacheList.RemoveAt(index);
        cacheList.Insert(index, sshKey);
        _cache = cacheList;
    }
    public void UpdateKeys(IMultiKeyConnectionCredentials multiKeyConnectionCredentials)
    {
        foreach (var key in multiKeyConnectionCredentials.Keys)
        {
            UpdateKey(key);
        }
    }
    
    public IEnumerable<ISshKey> GetAllKeys(bool loadFromDisk = false)
    {
        if (!loadFromDisk && _cache.Any())
        {
            foreach (var key in _cache) yield return key;
            yield break;
        }

        if (_cache.Any()) _cache = [];
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
                    if (settingsFile.ConvertPpkAutomatically) key = key.Convert(SshKeyFormat.OpenSSH, true, logger);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while reading key from {path}", filePath);
            }

            if (key is null) continue;
            _cache = _cache.Append(key);
            yield return key;
        }
    }
}