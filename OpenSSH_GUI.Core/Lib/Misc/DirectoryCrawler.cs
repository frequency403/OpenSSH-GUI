#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:27

#endregion

using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenSSH_GUI.Core.Database.Context;
using OpenSSH_GUI.Core.Database.DTO;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Interfaces.Credentials;
using OpenSSH_GUI.Core.Interfaces.Keys;
using OpenSSH_GUI.Core.Interfaces.Settings;
using OpenSSH_GUI.Core.Lib.Abstract;
using OpenSSH_GUI.Core.Lib.Settings;
using OpenSSH_GUI.Core.Lib.Static;
using SshNet.Keygen;

namespace OpenSSH_GUI.Core.Lib.Misc;

/// <summary>
/// Represents a directory crawler for searching and managing SSH keys.
/// </summary>
public static class DirectoryCrawler
{
    private static ILogger _logger;

    public static void ProvideContext(ILogger logger)
    {
        _logger = logger;
    }

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
                if (key is IPpkKey && convert) key = key.Convert(SshKeyFormat.OpenSSH, true, _logger);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while reading key from {path}", filePath);
            }

            if (key is null) continue;
            yield return key;
        }
    }

    private static SemaphoreSlim _semaphoreSlim = new(1, 1);

    /// <summary>
    /// Retrieves all SSH keys from disk or cache.
    /// </summary>
    /// <param name="loadFromDisk">Optional. Indicates whether to load keys from disk. Default is false.</param>
    /// <param name="purgeDtos">Optional. Indicates whether to purge the DTO's from the database. Default is false.</param>
    /// <returns>An enumerable collection of ISshKey representing the SSH keys.</returns>
    public static IEnumerable<ISshKey> GetAllKeys(bool loadFromDisk = false)
    {
        ISshKey[] keys = [];
        try
        {
            _semaphoreSlim.Wait();
            var dbContext = new OpenSshGuiDbContext();
            var cacheHasElements = dbContext.KeyDtos.Any();

            switch (loadFromDisk)
            {
                case false when cacheHasElements:
                    return dbContext.KeyDtos.Select(e => e.ToKey());
                case false when !cacheHasElements:
                    loadFromDisk = true;
                    break;
            }

            if (loadFromDisk || !cacheHasElements)
            {
                keys = GetFromDisk(dbContext.Settings.First().ConvertPpkAutomatically).ToArray();
                foreach (var key in keys)
                {
                    var found = dbContext.KeyDtos.FirstOrDefault(e =>
                        e.AbsolutePath == key.AbsoluteFilePath);
                    if (found is not null) continue;
                    dbContext.KeyDtos.Add(new SshKeyDto
                    {
                        AbsolutePath = key.AbsoluteFilePath,
                        Password = key.Password,
                        Format = key.Format
                    });
                }

                dbContext.SaveChanges();
            }
        }
        finally
        {
            _semaphoreSlim.Release();
        }

        return keys;
    }
}