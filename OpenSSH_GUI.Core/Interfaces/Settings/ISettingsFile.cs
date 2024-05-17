#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:34

#endregion

using OpenSSH_GUI.Core.Interfaces.Credentials;
using OpenSSH_GUI.Core.Lib.Credentials;
using OpenSSH_GUI.Core.Lib.Settings;

namespace OpenSSH_GUI.Core.Interfaces.Settings;

/// <summary>
/// Represents a settings file for the application.
/// </summary>
public interface ISettingsFile
{
    /// <summary>
    /// Represents the version of the settings file.
    /// </summary>
    string Version { get; set; }

    /// <summary>
    /// Gets or sets a flag indicating whether to automatically convert PPK keys.
    /// </summary>
    /// <value>
    /// <c>true</c> if PPK keys should be automatically converted; otherwise, <c>false</c>.
    /// </value>
    bool ConvertPpkAutomatically { get; set; }

    /// <summary>
    /// Represents the maximum number of saved servers in the settings file.
    /// </summary>
    /// <value>
    /// The maximum number of saved servers.
    /// </value>
    int MaxSavedServers { get; set; }

    /// <summary>
    /// Gets or sets the list of last used servers.
    /// </summary>
    List<IConnectionCredentials> LastUsedServers { get; set; }
}