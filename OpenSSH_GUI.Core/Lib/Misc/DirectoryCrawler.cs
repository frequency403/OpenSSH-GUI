using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Lib.Keys;
using OpenSSH_GUI.SshConfig;

namespace OpenSSH_GUI.Core.Lib.Misc;

/// <summary>
///     Represents a directory crawler for searching and managing SSH keys.
/// </summary>
public class DirectoryCrawler(
    ILogger<DirectoryCrawler> logger,
    IServiceProvider serviceProvider)
{
    private static readonly string[] ImportantFileNames = Enum.GetNames<SshConfigFiles>();

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

    /// <summary>
    ///     Retrieves new SSH key files from the disk asynchronously and yields them as an async enumerable.
    ///     Filters out important configuration files and considers only valid SSH key files for processing.
    /// </summary>
    /// <param name="token">A cancellation token that can be used to monitor for cancellation requests.</param>
    /// <returns>
    ///     An asynchronous enumerable of <see cref="SshKeyFile" /> objects representing discovered SSH key files.
    /// </returns>
    public async IAsyncEnumerable<SshKeyFile> GetNewFromDiskAsyncEnumerable(
        [EnumeratorCancellation] CancellationToken token = default)
    {
        var possibleKeyFiles = new List<string>();

        try
        {
            var file = SshConfigFiles.Config.GetPathOfFile();
            if (File.Exists(file))
            {
                await using var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read);
                using var streamReader = new StreamReader(fileStream);
                var fileContent = await streamReader.ReadToEndAsync(token);
                if (!string.IsNullOrWhiteSpace(fileContent))
                    foreach (var identityFileName in SshConfigParser.Parse(fileContent)
                                 .Blocks.SelectMany(e => e.GetEntries("IdentityFile")).Select(e => e.Value))
                    {
                        if (string.IsNullOrWhiteSpace(identityFileName)) continue;
                        possibleKeyFiles.Add(identityFileName);
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
                if (GenerateKeyFile() is not { } keyFileGenerated) continue;

                await keyFileGenerated.Load(possibleKeyFile);
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

}