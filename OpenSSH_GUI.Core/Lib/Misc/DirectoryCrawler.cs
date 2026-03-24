using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Lib.Keys;
using OpenSSH_GUI.SshConfig.Models;

namespace OpenSSH_GUI.Core.Lib.Misc;

/// <summary>
///     Represents a directory crawler for searching and managing SSH keys.
/// </summary>
public class DirectoryCrawler(
    ILogger<DirectoryCrawler> logger,
    IConfiguration configuration)
{
    private static readonly string[] ImportantFileNames = Enum.GetNames<SshConfigFiles>();
    
    /// <summary>
    ///     Asynchronously retrieves a collection of new SSH keys from the disk.
    /// </summary>
    /// <param name="token">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>An asynchronous enumerable containing the file paths of the discovered SSH keys.</returns>
    public ValueTask<IEnumerable<SshKeyFileSource>> GetPossibleKeyFilesOnDisk(CancellationToken token = default)
    {
        try
        {
            var possibleKeyFiles = new List<SshKeyFileSource>();

            try
            {
                if (configuration.GetSection("SshConfig").Get<SshConfiguration>() is { } sshConfig)
                {
                    foreach (var hostSetting in sshConfig.Hosts.Concat(sshConfig.Blocks).Append(sshConfig.Global))
                    {
                        if (hostSetting.IdentityFiles is not { Length: > 0 } hostIdentityFiles) continue;
                        foreach (var hostIdentityFile in hostIdentityFiles.Select(path => path.ResolvePath()))
                        {
                            if(!possibleKeyFiles.Any(e => e.AbsolutePath.Equals(hostIdentityFile, StringComparison.OrdinalIgnoreCase)) && File.Exists(hostIdentityFile))
                                possibleKeyFiles.Add(SshKeyFileSource.FromConfig(hostIdentityFile));
                        }
                    }
                }
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
                    .Where(e => !possibleKeyFiles.Any(k => k.AbsolutePath.Equals(e.FullName, StringComparison.OrdinalIgnoreCase)))
                    .Where(e => string.IsNullOrWhiteSpace(e.Extension) || e.Extension.Equals(".ppk", StringComparison.OrdinalIgnoreCase))
                    .DistinctBy(e => e.FullName, StringComparer.OrdinalIgnoreCase).Select(e => SshKeyFileSource.FromDisk(e.FullName))
            ).ToList();

            logger.LogInformation("Found {count} keys", possibleKeyFiles.Count);
            return ValueTask.FromResult<IEnumerable<SshKeyFileSource>>(possibleKeyFiles);
        }
        catch (Exception exception)
        {
            return ValueTask.FromException<IEnumerable<SshKeyFileSource>>(exception);
        }
    }
}