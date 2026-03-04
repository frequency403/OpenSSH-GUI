#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:27

#endregion

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Database.Context;
using OpenSSH_GUI.Core.Database.DTO;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Interfaces.Keys;
using OpenSSH_GUI.Core.Lib.Static;
using OpenSSH_GUI.SshConfig;
using Org.BouncyCastle.Crypto.Digests;

namespace OpenSSH_GUI.Core.Lib.Misc;

/// <summary>
///     Represents a directory crawler for searching and managing SSH keys.
/// </summary>
public class DirectoryCrawler(ILogger<DirectoryCrawler> logger, OpenSshGuiDbContext dbContext, IServiceProvider serviceProvider)
{
    private static string[] ImportantFileNames = Enum.GetNames<SshConfigFiles>();
    
    private SshKeyFile? GenerateKeyFile()
    {
        SshKeyFile? file = null;
        try
        {
            file = serviceProvider.GetService<SshKeyFile>();
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error resolving generic SshKeyFile");
        }
        return file;
    }

    public IEnumerable<SshKeyFile> GetNewFromDisk()
    {
        var possibleKeyFiles = new List<string>();

        try
        {
            var file = SshConfigFiles.Config.GetPathOfFile();
            if (File.Exists(file))
            {
                var fileContent = File.OpenText(file).ReadToEnd();
                if (!string.IsNullOrWhiteSpace(fileContent))
                {
                    foreach (var identityFileName in SshConfigParser.Parse(fileContent)
                                 .Blocks.SelectMany(e => e.GetEntries("IdentityFile")).Select(e => e.Value))
                    {
                        if (string.IsNullOrWhiteSpace(identityFileName)) continue;
                        possibleKeyFiles.Add(identityFileName);
                    }
                }
            }
            
            
        }
        catch (FileNotFoundException foundException)
        {
            logger.LogError(foundException, "Configuration file not found");
        }
        catch (Exception e)
        {
            logger.LogDebug(e, "Config not readable");
        }

        possibleKeyFiles = possibleKeyFiles.Concat(
            Directory.EnumerateFiles(SshConfigFilesExtension.GetBaseSshPath(), "*", new EnumerationOptions
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = false
            }).Select(e => new FileInfo(e))
                .Where(e => !ImportantFileNames.Any(ifn => ifn.Equals(e.Name, StringComparison.OrdinalIgnoreCase)))
            .Where(e => string.IsNullOrWhiteSpace(e.Extension))
                .DistinctBy(e => e.FullName, StringComparer.OrdinalIgnoreCase).Select(e => e.FullName)
        ).ToList();
        var keyFileCount = 0;
        foreach (var possibleKeyFile in possibleKeyFiles)
        {
            SshKeyFile? keyFile = null;
            try
            {
                if (GenerateKeyFile() is not { } keyFileGenerated) 
                {
                    continue;
                }
                keyFileGenerated.Load(possibleKeyFile);
                keyFile = keyFileGenerated;
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error loading keyfile {filePath}", possibleKeyFile);
            }

            if (keyFile is null) continue;
            keyFileCount++;
            yield return keyFile;
        }
        logger.LogInformation("Found {count} keys", keyFileCount);
    }
    
    /// <summary>
    ///     Retrieves SSH keys from disk.
    /// </summary>
    /// <param name="convert">
    ///     Optional. Indicates whether to automatically convert PuTTY keys to OpenSSH format. Default is
    ///     false.
    /// </param>
    /// <returns>An enumerable collection of ISshKey representing the SSH keys.</returns>
    private IEnumerable<ISshKey> GetFromDisk(bool convert)
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
                logger.LogError(ex, "Error while reading key from {path}", filePath);
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
    public async IAsyncEnumerable<ISshKey> GetAllKeysYield(bool loadFromDisk = false,
        bool purgePasswords = false)
    {
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
}