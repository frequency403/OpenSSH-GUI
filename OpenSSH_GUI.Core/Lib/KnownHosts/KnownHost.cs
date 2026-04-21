using System.Collections.ObjectModel;
using System.Text;
using DynamicData;
using OpenSSH_GUI.Core.Extensions;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace OpenSSH_GUI.Core.Lib.KnownHosts;

/// <summary>
///     Represents a known host in the OpenSSH GUI.
/// </summary>
public partial record KnownHost : ReactiveRecord
{
    /// <summary>
    ///     Represents a known host in the OpenSSH_GUI.
    /// </summary>
    [ReactiveCollection] private ObservableCollection<KnownHostKey> _keys = [];

    public KnownHostHost HostUri { get; }

    public KnownHost(KeyValuePair<KnownHostHost, KnownHostKey[]> knownHosts)
    {
        HostUri = knownHosts.Key;
        Keys.AddRange(knownHosts.Value);
    }
    
    public KnownHost(KnownHostHost uri, KnownHostKey[] keys)
    {
        HostUri = uri;
        Keys.AddRange(keys);
    }
    
    /// <summary>
    ///     Represents a known host entry in the known_hosts file.
    /// </summary>
    public KnownHost(IGrouping<string, string> knownHosts)
    {
        HostUri = new KnownHostHost(knownHosts.Key);
        Keys = new ObservableCollection<KnownHostKey>(knownHosts.Select(e =>
            new KnownHostKey(e.Replace($"{Host}", "").Trim())));
    }

    /// <summary>
    ///     Gets or sets the toggled state of the switch.
    /// </summary>
    /// <remarks>
    ///     When the switch is toggled:
    ///     - If it was previously off, all known host keys are marked for deletion.
    ///     - If it was previously on, all known host keys are unmarked for deletion.
    /// </remarks>
    [Reactive] private bool _switchToggled;

    /// <summary>
    ///     Represents a known host in the SSH known hosts file.
    /// </summary>
    public string Host => HostUri.ToString();

    /// <summary>
    ///     Represents a known host that can be deleted in its entirety.
    /// </summary>
    public bool DeleteWholeHost => Keys.All(e => e.MarkedForDeletion);

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
    public string Export(PlatformID? platformId = null)
    {
        platformId ??= Environment.OSVersion.Platform;
        if(DeleteWholeHost) return platformId.Value.GetLineSeparator();
        var stringBuilder = new StringBuilder();
        foreach (var knownHostKey in Keys.Where(e => !e.MarkedForDeletion))
        {
            stringBuilder.Append($"{Host} {knownHostKey}");
        }
        return stringBuilder.ToString();
    }
}