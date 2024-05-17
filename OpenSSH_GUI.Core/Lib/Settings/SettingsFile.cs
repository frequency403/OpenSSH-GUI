#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:29

#endregion

using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using OpenSSH_GUI.Core.Interfaces.Credentials;
using OpenSSH_GUI.Core.Interfaces.Settings;
using OpenSSH_GUI.Core.Lib.Credentials;

namespace OpenSSH_GUI.Core.Lib.Settings;

/// <summary>
/// Represents a settings file for the application.
/// </summary>
public record SettingsFile : ISettingsFile
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    /// <summary>
    /// The current version of the application.
    /// </summary>
    public string Version { get; set; } =
        $"v{(Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly()).GetName().Version.ToString(3)}";

    /// <summary>
    /// Whether to convert ppk files automatically.
    /// </summary>
    public bool ConvertPpkAutomatically { get; set; } = false;

    /// <summary>
    /// The maximum number of saved servers.
    /// </summary>
    public int MaxSavedServers { get; set; } = 5;

    /// <summary>
    /// The list of last used server connection credentials.
    /// </summary>
    public List<IConnectionCredentials> LastUsedServers { get; set; } = new ();
}