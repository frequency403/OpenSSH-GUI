﻿using OpenSSHALib.Interfaces;
using ReactiveUI;

namespace OpenSSHALib.Models;

public class KnownHost : ReactiveObject, IKnownHost
{
    private List<IKnownHostKey> _keys = [];

    public KnownHost(IGrouping<string, string> knownHosts)
    {
        Host = knownHosts.Key;
        Keys = knownHosts.Select(e => new KnownHostKey(e.Replace($"{Host}", "").Trim()) as IKnownHostKey).ToList();
    }

    public string Host { get; }
    public bool DeleteWholeHost => Keys.All(e => e.MarkedForDeletion);

    public List<IKnownHostKey> Keys
    {
        get => _keys;
        set => this.RaiseAndSetIfChanged(ref _keys, value);
    }

    private bool SwitchToggled { get; set; }

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