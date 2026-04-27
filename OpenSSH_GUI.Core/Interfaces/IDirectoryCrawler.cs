using OpenSSH_GUI.Core.Lib.Keys;

namespace OpenSSH_GUI.Core.Interfaces;

/// <summary>
///     Defines the contract for discovering SSH key file sources on disk.
/// </summary>
public interface IDirectoryCrawler
{
    /// <summary>
    ///     Gets a value indicating whether a key file search is currently in progress.
    /// </summary>
    bool IsSearching { get; }

    /// <summary>
    ///     Asynchronously enumerates possible SSH key file sources from both
    ///     the SSH configuration and the base SSH directory on disk.
    /// </summary>
    /// <param name="cancellationToken">Token to cancel the enumeration.</param>
    /// <returns>An async stream of discovered <see cref="SshKeyFileSource" /> instances.</returns>
    IAsyncEnumerable<SshKeyFileSource> GetPossibleKeyFilesOnDiskAsyncEnumerable(
        CancellationToken cancellationToken = default);
}