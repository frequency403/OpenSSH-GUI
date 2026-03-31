using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace OpenSSH_GUI.Core.Lib.KnownHosts;

/// <summary>
///     Represents a known host in the OpenSSH GUI.
/// </summary>
public partial record KnownHost : ReactiveRecord
{
    /// <summary>
    ///     Represents a known host entry in the known_hosts file.
    /// </summary>
    public KnownHost(IGrouping<string, string> knownHosts)
    {
        Host = knownHosts.Key;
        Keys = knownHosts.Select(e => new KnownHostKey(e.Replace($"{Host}", "").Trim()) as KnownHostKey).ToList();
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
    [Reactive]
    private List<KnownHostKey> _keys;

    /// <summary>
    ///     Toggles the marked for deletion flag of each <see cref="KnownHostKey" /> within the <see cref="Keys" /> list.
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
            ? KnownHostsFile.LineEnding
            : Keys
                .Where(e => !e.MarkedForDeletion)
                .Aggregate("",
                    (current, knownHostsKey) =>
                        current + $"{Host} {knownHostsKey.EntryWithoutHost}{KnownHostsFile.LineEnding}");
    }
}