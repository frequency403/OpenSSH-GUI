#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:27

#endregion

using DynamicData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Database.Context;
using OpenSSH_GUI.Core.Database.DTO;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Interfaces.Credentials;
using OpenSSH_GUI.Core.Interfaces.Keys;
using OpenSSH_GUI.Core.Interfaces.Settings;
using OpenSSH_GUI.Core.Lib.Keys;
using OpenSSH_GUI.Core.Lib.Settings;
using SshNet.Keygen;
using PpkKey = OpenSSH_GUI.Core.Lib.Keys.PpkKey;

namespace OpenSSH_GUI.Core.Lib.Misc;

/// <summary>
/// Represents a directory crawler for searching and managing SSH keys.
/// </summary>
public class DirectoryCrawler(ILogger<DirectoryCrawler> logger, OpenSshGuiDbContext context)
{
    /// <summary>
    /// Represents a collection of SSH keys used by the DirectoryCrawler class.
    /// </summary>
    private IEnumerable<ISshKey> _cache = context.KeyDtos.Select(e => e.ToKey());

    /// <summary>
    /// Refreshes the list of SSH keys by retrieving them from the cache or loading them from disk if specified.
    /// </summary>
    /// <returns>An IEnumerable of ISshKey objects representing the SSH keys.</returns>
    public IEnumerable<ISshKey> Refresh()
    {
        return GetAllKeys(true);
    }

    /// <summary>
    /// Updates the given SSH key in the cache.
    /// </summary>
    /// <param name="sshKey">The SSH key to update.</param>
    /// <remarks>
    /// <para>
    /// This method updates the provided SSH key in the cache. If the key is not found in the
    /// cache, it is added to the cache. If the key is already present in the cache, it is
    /// replaced with the updated key.
    /// </para>
    /// <para>
    /// The cache is an internal collection of SSH keys. The updated key is identified using
    /// the <see cref="ISshKey.AbsoluteFilePath"/> property. If no key is found with the
    /// same absolute file path, the updated key is added to the cache. Otherwise, the
    /// existing key with the same absolute file path is replaced with the updated key.
    /// </para>
    /// </remarks>
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

    /// <summary>
    /// Updates the SSH keys for a multi-key connection credentials object.
    /// </summary>
    /// <param name="multiKeyConnectionCredentials">The multi-key connection credentials object.</param>
    public void UpdateKeys(IMultiKeyConnectionCredentials multiKeyConnectionCredentials)
    {
        foreach (var key in multiKeyConnectionCredentials.Keys)
        {
            UpdateKey(key);
        }
    }

    private IEnumerable<ISshKey> GetFromDisk()
    {
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
                    if (context.Settings.AsNoTracking().First().ConvertPpkAutomatically) key = key.Convert(SshKeyFormat.OpenSSH, true, logger);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error while reading key from {path}", filePath);
            }

            if (key is null) continue;
            yield return key;
        }
    }
    
    /// <summary>
    /// Retrieves all SSH keys from disk or cache.
    /// </summary>
    /// <param name="loadFromDisk">Optional. Indicates whether to load keys from disk. Default is false.</param>
    /// <returns>An enumerable collection of ISshKey representing the SSH keys.</returns>
    public IEnumerable<ISshKey> GetAllKeys(bool loadFromDisk = false)
    {
        if (!loadFromDisk && _cache.Any())
        {
            return _cache;
        }
        
        var oldKeys = context.KeyDtos.ToList();

        foreach (var oldKey in oldKeys)
        {
            context.KeyDtos.Remove(oldKey);
        }

        if (oldKeys.Any())
        {
            context.SaveChanges();
        }

        if (loadFromDisk || oldKeys.Any() || !_cache.Any())
        {
            var keys = GetFromDisk();
            foreach (var key in keys)
            {
                var found = context.KeyDtos.Find(key.AbsoluteFilePath);
                var keyDto = new SshKeyDto
                {
                    AbsolutePath = key.AbsoluteFilePath,
                    Password = key.Password,
                    Format = key.Format
                };
                if (found is null)
                {
                    context.KeyDtos.Add(keyDto);
                }
                else
                {
                    found = keyDto;
                }
            }

            context.SaveChanges();
        }

        _cache = context.KeyDtos.Select(e => e.ToKey()).ToList();
        return _cache;
    }
}