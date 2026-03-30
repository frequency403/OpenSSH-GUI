using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Lib.Keys;
using OpenSSH_GUI.SshConfig.Models;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace OpenSSH_GUI.Core.Lib.Misc;

/// <summary>
///     Represents a directory crawler for searching and managing SSH keys.
/// </summary>
public partial class DirectoryCrawler(
    ILogger<DirectoryCrawler> logger,
    IConfiguration configuration) : ReactiveObject
{
    private static readonly string[] ImportantFileNames = Enum.GetNames<SshConfigFiles>();
    private static readonly List<SshKeyFileSource> keyFileSources = new();

    [Reactive] private bool _isSearching;
    
    /// <summary>
    ///     Asynchronously retrieves a collection of new SSH keys from the disk.
    /// </summary>
    /// <returns>An enumerable containing the file paths of the discovered SSH keys.</returns>
    public IEnumerable<SshKeyFileSource> GetPossibleKeyFilesOnDisk()
    {
        IsSearching = true;
        if (configuration.GetSection("SshConfig").Get<SshConfiguration>() is { } sshConfig)
        {
            foreach (var hostSetting in sshConfig.Hosts.Concat(sshConfig.Blocks).Append(sshConfig.Global))
            {
                if (hostSetting.IdentityFiles is not { Length: > 0 } hostIdentityFiles) continue;
                foreach (var hostIdentityFile in hostIdentityFiles.Select(path => path.ResolvePath()))
                {
                    if (!keyFileSources.Any(e =>
                            e.AbsolutePath.Equals(hostIdentityFile, StringComparison.OrdinalIgnoreCase)) &&
                        File.Exists(hostIdentityFile))
                    {
                        var source = SshKeyFileSource.FromConfig(hostIdentityFile);
                        logger.LogDebug("Adding key file source {Source}", source);
                        keyFileSources.Add(source);
                        yield return source;
                    }
                }
            }
        }
        
        foreach (var keyFileSource in Directory.EnumerateFiles(SshConfigFilesExtension.GetBaseSshPath(), "*",
                         new EnumerationOptions
                         {
                             IgnoreInaccessible = true,
                             RecurseSubdirectories = false
                         }).Select(e => new FileInfo(e))
                     .Where(e => !ImportantFileNames.Any(ifn =>
                         ifn.Equals(e.Name, StringComparison.OrdinalIgnoreCase)))
                     .Where(e => !keyFileSources.Any(k =>
                         k.AbsolutePath.Equals(e.FullName, StringComparison.OrdinalIgnoreCase)))
                     .Where(e => string.IsNullOrWhiteSpace(e.Extension) ||
                                 e.Extension.Equals(".ppk", StringComparison.OrdinalIgnoreCase))
                     .DistinctBy(e => e.FullName, StringComparer.OrdinalIgnoreCase)
                     .Select(e => SshKeyFileSource.FromDisk(e.FullName)))
        {
            logger.LogDebug("Adding keyfile {keyFile}", keyFileSource);
            keyFileSources.Add(keyFileSource);
            yield return keyFileSource;
        }
        keyFileSources.Clear();
        IsSearching = false;
    }
}