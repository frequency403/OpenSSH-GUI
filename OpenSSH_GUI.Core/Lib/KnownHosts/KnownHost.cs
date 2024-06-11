#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:29

#endregion

using OpenSSH_GUI.Core.Interfaces.KnownHosts;
using ReactiveUI;

namespace OpenSSH_GUI.Core.Lib.KnownHosts;

/// <summary>
///     Represents a known host in the OpenSSH GUI.
/// </summary>
public class KnownHost : ReactiveObject, IKnownHost
{
    /// The _keys variable represents a collection of known host keys for a specific host.
    /// It is an instance variable of the KnownHost class.
    /// The _keys variable is a list of IKnownHostKey objects that store information about individual host keys.
    /// Each IKnownHostKey object represents a known host key and provides access to its properties such as key type, fingerprint, and deletion status.
    /// Usage:
    /// var knownHost = new KnownHost(knownHosts);
    /// knownHost.Keys = _keys; // Set the list of known host keys
    /// See IKnownHostKey and KnownHost classes for more information.
    /// /
    private List<IKnownHostKey> _keys = [];

    /// <summary>
    ///     Represents a known host entry in the known_hosts file.
    /// </summary>
    public KnownHost(IGrouping<string, string> knownHosts)
    {
        Host = knownHosts.Key;
        Keys = knownHosts.Select(e => new KnownHostKey(e.Replace($"{Host}", "").Trim()) as IKnownHostKey).ToList();
    }

    /// <summary>
    ///     Gets or sets the toggled state of the switch.
    /// </summary>
    /// <remarks>
    ///     When the switch is toggled:
    ///     - If it was previously off, all known host keys are marked for deletion.
    ///     - If it was previously on, all known host keys are unmarked for deletion.
    /// </remarks>
    private bool SwitchToggled { get; set; }

    /// <summary>
    ///     Represents a known host in the SSH known hosts file.
    /// </summary>
    public string Host { get; }

    /// <summary>
    ///     Represents a known host that can be deleted in its entirety.
    /// </summary>
    public bool DeleteWholeHost => Keys.All(e => e.MarkedForDeletion);

    /// <summary>
    ///     Represents a known host in the OpenSSH_GUI.
    /// </summary>
    public List<IKnownHostKey> Keys
    {
        get => _keys;
        set => this.RaiseAndSetIfChanged(ref _keys, value);
    }

    /// <summary>
    ///     Toggles the marked for deletion flag of each <see cref="IKnownHostKey" /> within the <see cref="Keys" /> list.
    ///     If the <see cref="SwitchToggled" /> property is true, it sets the flag to false for all keys. Otherwise, it sets
    ///     the flag to true for all keys.
    /// </summary>
    public void KeysDeletionSwitch()
    {
        if (SwitchToggled)
        {
            foreach (var key in Keys) key.MarkedForDeletion = false;

            SwitchToggled = false;
        }
        else
        {
            foreach (var key in Keys) key.MarkedForDeletion = true;

            SwitchToggled = true;
        }
    }

    /// <summary>
    ///     Retrieves all entries for a known host in the known hosts file.
    /// </summary>
    /// <returns>
    ///     Returns a string containing all the entries for the known host.
    ///     If the entire host is marked for deletion, returns the line ending character.
    /// </returns>
    public string GetAllEntries()
    {
        return DeleteWholeHost
            ? IKnownHostsFile.LineEnding
            : Keys
                .Where(e => !e.MarkedForDeletion)
                .Aggregate("",
                    (current, knownHostsKey) =>
                        current + $"{Host} {knownHostsKey.EntryWithoutHost}{IKnownHostsFile.LineEnding}");
    }
}