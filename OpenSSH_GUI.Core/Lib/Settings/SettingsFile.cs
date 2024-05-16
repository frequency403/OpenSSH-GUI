#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:29

#endregion

using System.Diagnostics;
using System.Reflection;
using OpenSSH_GUI.Core.Interfaces.Credentials;
using OpenSSH_GUI.Core.Interfaces.Settings;
using OpenSSH_GUI.Core.Lib.Settings.Event;

namespace OpenSSH_GUI.Core.Lib.Settings
{
    /// <summary>
    /// Represents a settings file for the application.
    /// </summary>
    [Serializable]
    public record SettingsFile : ISettingsFile
    {
        /// <summary>
        /// Delegate for the SettingsChanged event handler.
        /// </summary>
        /// <param name="sender">The object that is triggering the event.</param>
        /// <param name="e">The settings changed event arguments.</param>
        public delegate SettingsChangedEventArgs SettingsChangedEventHandler(object sender, SettingsChangedEventArgs e);

        /// <summary>
        /// Event triggered when a setting is changed.
        /// </summary>
        public event SettingsChangedEventHandler SettingsChanged;

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
        public List<IConnectionCredentials> LastUsedServers { get; set; } = new List<IConnectionCredentials>();

        /// <summary>
        /// Change the settings with values from another settings file instance.
        /// </summary>
        /// <param name="settingsFile">The other settings file instance.</param>
        public void ChangeSettings(ISettingsFile settingsFile)
        {
            Version = settingsFile.Version;
            ConvertPpkAutomatically = settingsFile.ConvertPpkAutomatically;
            MaxSavedServers = settingsFile.MaxSavedServers;
            LastUsedServers = settingsFile.LastUsedServers;

            // Trigger settings changed event with the name of this method as the caller.
            RaiseSettingsChanged(nameof(ChangeSettings));
        }

        /// <summary>
        /// Raise the SettingsChanged event.
        /// </summary>
        /// <param name="caller">The name of the method triggering the changing settings.</param>
        protected virtual void RaiseSettingsChanged(string caller)
        {
            // Invoke the SettingsChanged event
            SettingsChanged?.Invoke(this, new SettingsChangedEventArgs { Caller = caller });
        }
    }
}