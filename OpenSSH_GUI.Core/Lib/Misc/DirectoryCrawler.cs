using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenSSH_GUI.Core.Configuration;
using OpenSSH_GUI.Core.Enums;
using OpenSSH_GUI.Core.Extensions;
using OpenSSH_GUI.Core.Interfaces;
using OpenSSH_GUI.Core.Lib.Keys;
using OpenSSH_GUI.SshConfig.Models;

namespace OpenSSH_GUI.Core.Lib.Misc;

/// <summary>
///     Represents a directory crawler for searching and managing SSH keys.
/// </summary>
public sealed class DirectoryCrawler(ILogger<DirectoryCrawler> logger, IConfiguration configuration, IMutableConfiguration<ApplicationConfiguration> mutableConfiguration) : IDirectoryCrawler
{
    private static readonly string[] ImportantFileNames = Enum.GetNames<SshConfigFiles>();
    private readonly List<SshKeyFileSource> _keyFileSources = [];

    public bool IsSearching { get; private set; } = false;

    /// <summary>
    ///     Asynchronously enumerates possible SSH key file sources from both
    ///     the SSH configuration and the base SSH directory on disk.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the enumeration.</param>
    /// <returns>An async stream of discovered <see cref="SshKeyFileSource" /> instances.</returns>
    public async IAsyncEnumerable<SshKeyFileSource> GetPossibleKeyFilesOnDiskAsyncEnumerable(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        IsSearching = true;

        try
        {
            if (configuration.GetSection("SshConfig").Get<SshConfiguration>() is { } sshConfig)
                foreach (var hostSetting in sshConfig.Hosts.Concat(sshConfig.Blocks).Append(sshConfig.Global))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (hostSetting.IdentityFiles is not { Length: > 0 } hostIdentityFiles)
                        continue;

                    foreach (var resolvedPath in hostIdentityFiles.Select(p => p.ResolvePath()))
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        var alreadyTracked = _keyFileSources.Any(e =>
                            e.AbsolutePath.Equals(resolvedPath, StringComparison.OrdinalIgnoreCase));

                        var exists = await Task.Run(() => File.Exists(resolvedPath), cancellationToken);

                        if (alreadyTracked || !exists)
                            continue;

                        var source = SshKeyFileSource.FromConfig(resolvedPath);
                        logger.LogDebug("Adding key file source {Source}", source);
                        _keyFileSources.Add(source);
                        yield return source;
                    }
                }

            await foreach (var keyFileSource in EnumerateDiskSources(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                logger.LogDebug("Adding keyfile {KeyFile}", keyFileSource);
                _keyFileSources.Add(keyFileSource);
                yield return keyFileSource;
            }
        }
        finally
        {
            _keyFileSources.Clear();
            IsSearching = false;
        }
    }

    /// <summary>
    ///     Enumerates SSH key file sources from the base SSH directory,
    ///     excluding already tracked and config-reserved files.
    /// </summary>
    /// <returns>A list of <see cref="SshKeyFileSource" /> found on disk.</returns>
    private async IAsyncEnumerable<SshKeyFileSource> EnumerateDiskSources([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var directoryInfo in mutableConfiguration.Current.LookupPaths.Select(e => new DirectoryInfo(e)) ?? [])
        {
            logger.LogDebug("Processing directory {Directory}", directoryInfo);
            if(cancellationToken.IsCancellationRequested)
                yield break;
            foreach (var keyFile in directoryInfo.EnumerateFiles("*", new EnumerationOptions
                     {
                         IgnoreInaccessible = true,
                         RecurseSubdirectories = false
                     }).Where(e => !ImportantFileNames.Any(ifn =>
                         ifn.Equals(e.Name, StringComparison.OrdinalIgnoreCase)))
                     .Where(e => !_keyFileSources.Any(k =>
                         k.AbsolutePath.Equals(e.FullName, StringComparison.OrdinalIgnoreCase)))
                     .Where(e => string.IsNullOrWhiteSpace(e.Extension) ||
                                 e.Extension.Equals(
                                     SshKeyFormatExtension.PuttyKeyFileExtension,
                                     StringComparison.OrdinalIgnoreCase))
                     .DistinctBy(e => e.FullName, StringComparer.OrdinalIgnoreCase))
            {
                logger.LogDebug("Found key file {KeyFile}", keyFile);
                yield return SshKeyFileSource.FromDisk(keyFile.FullName);
                if(cancellationToken.IsCancellationRequested)
                    yield break;
            }
        }
    }
}