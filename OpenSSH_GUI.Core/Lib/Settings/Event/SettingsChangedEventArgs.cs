// File Created by: Oliver Schantz
// Created: 15.05.2024 - 16:05:34
// Last edit: 15.05.2024 - 16:05:35

namespace OpenSSH_GUI.Core.Lib.Settings.Event;

/// <summary>
/// Represents the event arguments for the SettingsChanged event.
/// </summary>
public class SettingsChangedEventArgs
{
    /// <summary>
    /// Represents the caller of a settings change event.
    /// </summary>
    public string Caller { get; init; }
}