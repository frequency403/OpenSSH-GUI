#region CopyrightNotice

// File Created by: Oliver Schantz
// Created: 15.05.2024 - 00:05:44
// Last edit: 15.05.2024 - 01:05:29

#endregion

using OpenSSH_GUI.Core.Interfaces.KnownHosts;
using ReactiveUI;

namespace OpenSSH_GUI.Core.Lib.KnownHosts;

public class KnownHost : ReactiveObject, IKnownHost
{
    private List<IKnownHostKey> _keys = [];

    public KnownHost(IGrouping<string, string> knownHosts)
    {
        Host = knownHosts.Key;
        Keys = knownHosts.Select(e => new KnownHostKey(e.Replace($"{Host}", "").Trim()) as IKnownHostKey).ToList();
    }

    private bool SwitchToggled { get; set; }

    public string Host { get; }
    public bool DeleteWholeHost => Keys.All(e => e.MarkedForDeletion);

    public List<IKnownHostKey> Keys
    {
        get => _keys;
        set => this.RaiseAndSetIfChanged(ref _keys, value);
    }

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