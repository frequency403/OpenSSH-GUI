#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:35

#endregion

using System.Collections.ObjectModel;
using OpenSSH_GUI.Core.Interfaces.Keys;
using ReactiveUI;

namespace OpenSSH_GUI.Core.Interfaces.AuthorizedKeys;

/// <summary>
/// Interface for an Authorized Keys file.
/// </summary>
public interface IAuthorizedKeysFile : IReactiveObject
{
    /// <summary>
    /// Represents the authorized keys file.
    /// </summary>
    ObservableCollection<IAuthorizedKey> AuthorizedKeys { get; set; }

    /// <summary>
    /// Adds an authorized key to the authorized keys file.
    /// </summary>
    /// <param name="key">The SSH key to be added.</param>
    /// <returns>True if the key was successfully added, otherwise false.</returns>
    bool AddAuthorizedKey(ISshKey key);

    /// <summary>
    /// Applies the changes to the authorized keys file.
    /// </summary>
    /// <param name="keys">The collection of keys to be applied as changes.</param>
    /// <returns>True if any changes were made to the authorized keys file; otherwise, false.</returns>
    bool ApplyChanges(IEnumerable<IAuthorizedKey> keys);

    /// <summary>
    /// Persists the changes made to the authorized keys file.
    /// </summary>
    /// <returns>The modified <see cref="IAuthorizedKeysFile"/> object.</returns>
    IAuthorizedKeysFile PersistChangesInFile();

    /// <summary>
    /// Adds an authorized key asynchronously.
    /// </summary>
    /// <param name="key">The SSH key to be added.</param>
    /// <returns>A task representing the asynchronous operation. The task result is a boolean value indicating whether the key was added successfully.</returns>
    Task<bool> AddAuthorizedKeyAsync(ISshKey key);

    /// <summary>
    /// Removes the specified SSH key from the authorized keys list.
    /// </summary>
    /// <param name="key">The SSH key to remove.</param>
    /// <returns>
    /// Returns <c>true</c> if the key is successfully removed;
    /// otherwise, <c>false</c>.
    /// </returns>
    bool RemoveAuthorizedKey(ISshKey key);

    /// <summary>
    /// Exports the content of the authorized keys file.
    /// </summary>
    /// <param name="local">Indicates whether to export for the local machine or remote server. Default is true (local).</param>
    /// <param name="platform">The platform ID of the server. If null, the current OS platform will be used. Only applicable if 'local' is set to false.</param>
    /// <returns>The content of the authorized keys file as a string.</returns>
    string ExportFileContent(bool local = true, PlatformID? platform = null);
}