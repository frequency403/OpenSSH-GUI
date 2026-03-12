using System.Collections.ObjectModel;
using ReactiveUI;

namespace OpenSSH_GUI.Core.Interfaces.KnownHosts;

/// <summary>
///     Represents a known hosts file.
/// </summary>
public interface IKnownHostsFile : IReactiveObject
{
    /// <summary>
    ///     Represents the line ending character used in the known_hosts file.
    /// </summary>
    static string LineEnding { get; set; } = string.Empty;

    /// <summary>
    ///     Represents a file that contains known host entries.
    /// </summary>
    ObservableCollection<IKnownHost> KnownHosts { get; }

    /// <summary>
    ///     Asynchronously reads the contents of the known hosts file.
    /// </summary>
    /// <param name="stream">
    ///     The file stream to read from. If null, the method reads from the file specified in the
    ///     constructor.
    /// </param>
    /// <returns>A <see cref="ValueTask" /> representing the asynchronous operation.</returns>
    ValueTask ReadContentAsync(FileStream? stream = null);

    /// <summary>
    ///     Synchronizes the known hosts with the given list of new known hosts.
    /// </summary>
    /// <param name="newKnownHosts">The new known hosts to synchronize.</param>
    void SyncKnownHosts(IEnumerable<IKnownHost> newKnownHosts);

    /// <summary>
    ///     Updates the content of the known hosts file asynchronously.
    /// </summary>
    /// <returns>A <see cref="ValueTask" /> representing the update operation.</returns>
    ValueTask UpdateFileAsync();

    /// <summary>
    ///     Initializes the known hosts file asynchronously.
    /// </summary>
    /// <param name="knownHostsPathOrContent">The path to the known hosts file or its content.</param>
    /// <param name="fromServer">Indicates whether the content is from a server.</param>
    /// <param name="token">A cancellation token.</param>
    /// <returns>A <see cref="ValueTask{IKnownHostsFile}" /> representing the initialized object.</returns>
    ValueTask<IKnownHostsFile> InitializeAsync(string knownHostsPathOrContent, bool fromServer = false,
        CancellationToken token = default);

    /// <summary>
    ///     Retrieves the updated contents of the known hosts file.
    /// </summary>
    /// <param name="platformId">The platform ID of the server.</param>
    /// <returns>The updated contents of the known hosts file as a string.</returns>
    /// <remarks>
    ///     This method retrieves the updated contents of the known hosts file.
    ///     It takes the platform ID of the server as a parameter and returns the
    ///     updated contents of the known hosts file as a string. The method
    ///     checks if the instance of the KnownHostsFile class is created from
    ///     the server or not. If it is not created from the server, it returns
    ///     an empty string. It sets the LineEnding property based on the platform
    ///     ID provided. It then aggregates the known hosts entries excluding those
    ///     which are flagged for deletion and returns the updated contents as a string.
    /// </remarks>
    /// <param name="platformId">The platform ID of the server.</param>
    string GetUpdatedContents(PlatformID platformId);
}