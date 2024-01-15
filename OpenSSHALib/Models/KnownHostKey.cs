using OpenSSHALib.Enums;
using ReactiveUI;

namespace OpenSSHALib.Models;

public class KnownHostKey : ReactiveObject
{
    private bool _markedForDeletion;

    public KnownHostKey(string entry)
    {
        EntryWithoutHost = entry;
        var splitted = EntryWithoutHost.Split(' ');
        TypeDeclarationInFile = splitted[0];
        KeyType = Enum.Parse<KeyType>(
            TypeDeclarationInFile.StartsWith("ssh-")
                ? TypeDeclarationInFile.Replace("ssh-", "")
                : TypeDeclarationInFile.Split('-')[0], true);
        Fingerprint = splitted[1].Replace("\n", "").Replace("\r", "");
    }

    public KeyType KeyType { get; }
    private string TypeDeclarationInFile { get; }
    public string Fingerprint { get; }
    public string EntryWithoutHost { get; }

    public bool MarkedForDeletion
    {
        get => _markedForDeletion;
        set => this.RaiseAndSetIfChanged(ref _markedForDeletion, value);
    }
}