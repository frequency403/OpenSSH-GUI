using System.Text;
using OpenSSHALib.Enums;
using ReactiveUI;

namespace OpenSSHALib.Models;

public class KnownHostKey : ReactiveObject
{
    public KeyType KeyType { get; }
    private string TypeDeclarationInFile { get; }
    public string Fingerprint { get; }
    public string EntryWithoutHost { get; }

    private bool _markedForDeletion = false;
    public bool MarkedForDeletion
    {
        get => _markedForDeletion;
        set => this.RaiseAndSetIfChanged(ref _markedForDeletion, value);
    }
    
    
    public long KeySize => Convert.FromBase64String(Fingerprint).LongLength;

    public KnownHostKey(string entry)
    {
        EntryWithoutHost = entry;
        var splitted = EntryWithoutHost.Split(' ');
        TypeDeclarationInFile = splitted[0];
        KeyType = Enum.Parse<KeyType>(TypeDeclarationInFile.StartsWith("ssh-") ? TypeDeclarationInFile.Replace("ssh-", "") : TypeDeclarationInFile.Split('-')[0], true);
        Fingerprint = splitted[1];
    }
}