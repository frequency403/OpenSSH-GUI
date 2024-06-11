#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:29

#endregion

using System.Reflection;

namespace OpenSSH_GUI.Core.Lib.Settings;

/// <summary>
///     Represents a settings file for the application.
/// </summary>
public record Settings
{
    /// <summary>
    ///     The current version of the application.
    /// </summary>
    public string Version { get; set; } =
        $"v{(Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly()).GetName().Version?.ToString(3)}";

    /// <summary>
    ///     Whether to convert ppk files automatically.
    /// </summary>
    public bool ConvertPpkAutomatically { get; set; } = false;
}