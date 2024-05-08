using System.Collections.ObjectModel;
using OpenSSHALib.Models;
using ReactiveUI;

namespace OpenSSHALib.Interfaces;

public interface IKnownHostsFile : IReactiveObject
{
    static string LineEnding { get; set; }
    ObservableCollection<IKnownHost> KnownHosts { get; }
    Task ReadContentAsync(FileStream? stream = null);
    void SyncKnownHosts(IEnumerable<IKnownHost> newKnownHosts);
    Task UpdateFile();
    string GetUpdatedContents(PlatformID platformId);
}