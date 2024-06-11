#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:27

#endregion

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Database.Context;
using OpenSSH_GUI.Core.Database.DTO;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Interfaces.Keys;
using OpenSSH_GUI.Core.Lib.Static;

namespace OpenSSH_GUI.Core.Lib.Misc;

/// <summary>
///     Represents a directory crawler for searching and managing SSH keys.
/// </summary>
public static class DirectoryCrawler
{
    private static ILogger _logger = null!;

    /// <summary>
    ///     Provides the logger context for the DirectoryCrawler class.
    /// </summary>
    /// <param name="logger">The logger instance to be used by the DirectoryCrawler class.</param>
    public static void ProvideContext(ILogger logger)
    {
        _logger = logger;
    }

    /// <summary>
    ///     Retrieves SSH keys from disk.
    /// </summary>
    /// <param name="convert">
    ///     Optional. Indicates whether to automatically convert PuTTY keys to OpenSSH format. Default is
    ///     false.
    /// </param>
    /// <returns>An enumerable collection of ISshKey representing the SSH keys.</returns>
    private static IEnumerable<ISshKey> GetFromDisk(bool convert)
    {
        foreach (var filePath in Directory
                     .EnumerateFiles(SshConfigFilesExtension.GetBaseSshPath(), "*", SearchOption.TopDirectoryOnly)
                     .Where(e => e.EndsWith("pub") || e.EndsWith("ppk")))
        {
            ISshKey? key = null;
            try
            {
                key = KeyFactory.FromPath(filePath);
                if (key is IPpkKey && convert) key = KeyFactory.ConvertToOppositeFormat(key, true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while reading key from {path}", filePath);
            }

            if (key is null) continue;
            yield return key;
        }
    }

    /// <summary>
    ///     Retrieves all SSH keys from disk or cache asynchronously, using a yield return method to lazily load the keys.
    /// </summary>
    /// <param name="loadFromDisk">Optional. Indicates whether to load keys from disk. Default is false.</param>
    /// <param name="purgePasswords">Optional. Indicates whether to purge passwords from cache. Default is false.</param>
    /// <returns>An asynchronous enumerable collection of ISshKey representing the SSH keys.</returns>
    public static async IAsyncEnumerable<ISshKey> GetAllKeysYield(bool loadFromDisk = false,
        bool purgePasswords = false)
    {
        await using var dbContext = new OpenSshGuiDbContext();
        var cacheHasElements = await dbContext.KeyDtos.AnyAsync();
        switch (loadFromDisk)
        {
            case false when cacheHasElements:
                foreach (var keyFromCache in dbContext.KeyDtos.Select(e => e.ToKey())) yield return keyFromCache!;
                yield break;
            case false when !cacheHasElements:
                loadFromDisk = true;
                break;
        }

        if (!loadFromDisk && cacheHasElements) yield break;
        {
            foreach (var key in GetFromDisk((await dbContext.Settings.FirstAsync()).ConvertPpkAutomatically))
            {
                var found = await dbContext.KeyDtos.FirstOrDefaultAsync(e =>
                    e.AbsolutePath == key.AbsoluteFilePath);
                if (found is null)
                {
                    await dbContext.KeyDtos.AddAsync(new SshKeyDto
                    {
                        AbsolutePath = key.AbsoluteFilePath,
                        Password = key.Password,
                        Format = key.Format
                    });
                }
                else
                {
                    found.AbsolutePath = key.AbsoluteFilePath;
                    found.Format = key.Format;
                    if (found.Password is not null) key.Password = found.Password;
                    if (purgePasswords)
                    {
                        key.Password = key.HasPassword ? "" : null;
                        found.Password = key.HasPassword ? "" : null;
                    }
                }

                await dbContext.SaveChangesAsync();
                yield return key;
            }
        }
    }

    /// <summary>
    ///     Retrieves all SSH keys from disk or cache.
    /// </summary>
    /// <param name="loadFromDisk">Optional. Indicates whether to load keys from disk. Default is false.</param>
    /// <returns>An enumerable collection of ISshKey representing the SSH keys.</returns>
    public static IEnumerable<ISshKey> GetAllKeys(bool loadFromDisk = false)
    {
        return GetAllKeysYield(loadFromDisk).ToBlockingEnumerable();
    }
}