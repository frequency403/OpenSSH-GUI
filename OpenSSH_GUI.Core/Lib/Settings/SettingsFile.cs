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

namespace OpenSSH_GUI.Core.Lib.Settings;

[Serializable]
public record SettingsFile : ISettingsFile
{
    public delegate SettingsChangedEventArgs SettingsChangedEventHandler(object sender, SettingsChangedEventArgs e);
    public event SettingsChangedEventHandler SettingsChanged;

    public string Version { get; set; } =
        $"v{(Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly()).GetName().Version.ToString(3)}";
    public bool ConvertPpkAutomatically { get; set; } = false;
    public int MaxSavedServers { get; set; } = 5;
    public List<IConnectionCredentials> LastUsedServers { get; set; } = [];

    public void ChangeSettings(ISettingsFile settingsFile)
    {
        Version = settingsFile.Version;
        ConvertPpkAutomatically = settingsFile.ConvertPpkAutomatically;
        MaxSavedServers = settingsFile.MaxSavedServers;
        LastUsedServers = settingsFile.LastUsedServers;
        //@TODO: Slow AF
        RaiseSettingsChanged(nameof(ChangeSettings));
    }
    
    protected virtual void RaiseSettingsChanged(string caller)
    {
        SettingsChanged?.Invoke(this, new SettingsChangedEventArgs {Caller = caller});
    }
}