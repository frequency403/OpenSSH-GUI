using OpenSSHALib.Models;
using ReactiveUI;

namespace OpenSSHALib.Interfaces;

public interface IKnownHost : IReactiveObject
{
    string Host { get; }
    bool DeleteWholeHost { get; }
    List<IKnownHostKey> Keys { get; set; }
    void KeysDeletionSwitch();
    string GetAllEntries();
}