using ReactiveUI;

namespace OpenSSHALib.Models;

public class KnownHost : ReactiveObject
{
    public string Host { get; }
    public bool DeleteWholeHost => Keys.All(e => e.MarkedForDeletion);

    private List<KnownHostKey> _keys = [];
    public List<KnownHostKey> Keys
    {
        get => _keys;
        set => this.RaiseAndSetIfChanged(ref _keys, value);
    }
    
    public KnownHost(IGrouping<string, string> knownHosts)
    {
        Host = knownHosts.Key;
        Keys = knownHosts.Select(e => new KnownHostKey(e.Replace($"{Host}", "").Trim())).ToList();
    }

    private bool SwitchToggled { get; set; }
    public void KeysDeletionSwitch()
    {
        if (SwitchToggled)
        {
            foreach (var key in Keys) { key.MarkedForDeletion = false; }

            SwitchToggled = false;
        }
        else
        {
            foreach (var key in Keys) { key.MarkedForDeletion = true; }

            SwitchToggled = true;
        }
        
    }
    public string GetAllEntries() => DeleteWholeHost ? KnownHostsFile.LineEnding : Keys
        .Where(e => !e.MarkedForDeletion)
        .Aggregate("", (current, knownHostsKey) => current + $"{Host} {knownHostsKey.EntryWithoutHost}{KnownHostsFile.LineEnding}");
}