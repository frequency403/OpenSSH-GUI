#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:33

#endregion

using OpenSSH_GUI.Core.Interfaces.Credentials;
using OpenSSH_GUI.Core.Lib.Credentials;
using OpenSSH_GUI.Core.Lib.Misc;

namespace OpenSSH_GUI.Core.Interfaces.Settings;

/// IApplicationSettings interface provides methods for initializing application settings, adding a known server to a file, and saving the current settings to a file.
/// /
public interface IApplicationSettings
{
    /// <summary>
    /// Adds a known server to the settings file.
    /// </summary>
    /// <param name="credentials">The connection credentials of the server to add.</param>
    /// <returns>True if the server was added successfully, false otherwise.</returns>
    bool AddKnownServer(IConnectionCredentials credentials);

    /// <summary>
    /// Adds a known server to the settings file asynchronously.
    /// </summary>
    /// <param name="credentials">The connection credentials for the server.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains a boolean indicating whether the server was added successfully or not.</returns>
    Task<bool> AddKnownServerAsync(IConnectionCredentials credentials);
}