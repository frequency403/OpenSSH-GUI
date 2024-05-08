using OpenSSHALib.Enums;
using ReactiveUI;

namespace OpenSSHALib.Interfaces;

public interface IKnownHostKey : IReactiveObject
{
    KeyType KeyType { get; }
    string Fingerprint { get; }
    string EntryWithoutHost { get; }
    bool MarkedForDeletion { get; set; }
}