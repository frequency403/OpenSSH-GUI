#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:34

#endregion

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
    static string LineEnding { get; set; }

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
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ReadContentAsync(FileStream? stream = null);

    /// <summary>
    ///     Synchronizes the known hosts with the given list of new known hosts.
    /// </summary>
    /// <param name="newKnownHosts">The new known hosts to synchronize.</param>
    void SyncKnownHosts(IEnumerable<IKnownHost> newKnownHosts);

    /// <summary>
    ///     Updates the content of the known hosts file.
    /// </summary>
    /// <returns>A task representing the update operation.</returns>
    Task UpdateFile();

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